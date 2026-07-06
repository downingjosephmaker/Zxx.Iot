import { ref, reactive, computed } from "vue";

export type MqttConnectionStatus = 'disconnected' | 'connecting' | 'connected' | 'error';

/**
 * 设备数据接口(与静态数据格式一致)
 */
export interface DeviceData {
  DeviceName: string;
  DeviceTypeName: string;
  DeviceGuid: string;
  LastOnlineTime: string;
  DeviceState: string; // '在线' | '掉电' | '离线'
  DeviceAlarm: string; // '告警' | '正常'
  DeviceSwitch: string; // '开' | '关'
  DeviceParams: DeviceParam[];
}

/**
 * 设备参数接口
 */
export interface DeviceParam {
  ParamCode: string;
  ParamName: string;
  ValueUnit: string;
  CollectTime: string | null;
  ParamLastValue: string | null;
  ParamValue: string;
  IsAlarm: number; // 0: 正常, 1: 告警
}

/**
 * MQTT 消息数据格式(与 API 返回格式一致)
 */
export interface MqttMessageData {
  Result: string; // JSON 字符串,包含 DeviceData 数组
  Timestamp: string;
  Status: boolean;
  Message: string;
  Total: number;
}


/**
 * MQTT 配置接口(与数据集配置一致)
 */
export interface MqttConfig {
  host: string; // MQTT服务器地址,如 'mqtt://localhost:1883'
  clientId?: string; // 客户端ID
  username?: string; // 用户名
  password?: string; // 密码
  dataTopic?: string; // 实时数据主题
  alarmTopic?: string; // 告警主题
  qos?: '0' | '1' | '2'; // QoS等级
}


/**
 * MQTT数据服务
 * 负责MQTT连接管理、主题订阅和消息处理
 */
export class FuxaMqttService {
  // 连接状态
    // 当前MQTT配置
  private currentConfig: MqttConfig | null = null;

  private _status = ref<MqttConnectionStatus>('disconnected');
  private _error = ref<string>('');
  private _messageCount = ref<number>(0);

  // 设备数据存储(按主题分类)
  private _dataTopicDevices = reactive<Map<string, DeviceData[]>>(new Map());
  private _alarmTopicMessages = reactive<Map<string, any[]>>(new Map());

  // MQTT客户端实例(实际项目中使用 mqtt.js 库)
  private mqttClient: any = null;
  private reconnectTimer: NodeJS.Timeout | null = null;
  private maxReconnectAttempts = 5;
  private currentReconnectAttempts = 0;

  // 订阅的主题列表(由数据集配置)
  private subscribedTopics = new Set<string>();

  // 消息处理回调
  private messageHandlers = new Map<string, (data: any) => void>();

  // 数据更新回调
  private onDataUpdateCallback: ((topic: string, devices: DeviceData[]) => void) | null = null;
  private onAlarmUpdateCallback: ((topic: string, alarms: any[]) => void) | null = null;

  // 响应式属性
  public readonly status = computed(() => this._status.value);
  public readonly error = computed(() => this._error.value);
  public readonly messageCount = computed(() => this._messageCount.value);
  public readonly dataDevices = computed(() => this._dataTopicDevices);
  public readonly alarmMessages = computed(() => this._alarmTopicMessages);
  public readonly devices = computed(() => {
    // 兼容旧代码,返回第一个主题的设备数据
    const firstTopic = Array.from(this._dataTopicDevices.keys())[0];
    return this._dataTopicDevices.get(firstTopic) || [];
  });
  public readonly deviceCount = computed(() => {
    let count = 0;
    this._dataTopicDevices.forEach(devices => count += devices.length);
    return count;
  });

  /**
   * 连接到MQTT服务器(使用数据集配置)
   * @param config MQTT配置对象
   */
  async connect(config: MqttConfig): Promise<void> {
    if (this._status.value === 'connected' || this._status.value === 'connecting') {
      console.log('MQTT已连接或正在连接');
      return;
    }

    try {
      this._status.value = 'connecting';
      this._error.value = '';

      // 保存配置
      this.currentConfig = config;

      // 构建连接选项
      const brokerUrl = config.host || '';
      const options = {
        clientId: config.clientId || `scada_mqtt_${Date.now()}`,
        username: config.username || '',
        password: config.password || '',
        keepalive: 60,
        clean: true,
        reconnectPeriod: 5000
      };

      this.currentReconnectAttempts = 0;

      // 自动订阅数据集配置的主题
      await this.autoSubscribeTopics(config);

      console.log('MQTT连接成功:', brokerUrl);
    } catch (error) {
      this._status.value = 'error';
      this._error.value = error instanceof Error ? error.message : '连接失败';
      console.error('MQTT连接失败:', error);

      // 启动重连机制
      this.scheduleReconnect();
      throw error;
    }
  }

  /**
   * 自动订阅数据集配置的主题
   */
  private async autoSubscribeTopics(config: MqttConfig): Promise<void> {
    const qos = parseInt(config.qos || '1') as 0 | 1 | 2;

    // 订阅实时数据主题
    if (config.dataTopic) {
      await this.subscribeCustomTopic(config.dataTopic, qos);
      console.log(`已自动订阅实时数据主题: ${config.dataTopic}`);
    }

    // 订阅告警主题
    if (config.alarmTopic) {
      await this.subscribeCustomTopic(config.alarmTopic, qos);
      console.log(`已自动订阅告警主题: ${config.alarmTopic}`);
    }
  }

  /**
   * 断开MQTT连接
   */
  disconnect(): void {
    if (this.mqttClient) {
      this.mqttClient.end?.();
      this.mqttClient = null;
    }

    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer);
      this.reconnectTimer = null;
    }

    this._status.value = 'disconnected';
    this._error.value = '';
    this.currentReconnectAttempts = 0;

    // 清除数据
    this._dataTopicDevices.clear();
    this._alarmTopicMessages.clear();
    this.subscribedTopics.clear();
    this.messageHandlers.clear();
    this.currentConfig = null;

    console.log('MQTT连接已断开');
  }

  /**
   * 订阅自定义主题(用于数据集配置)
   * @param topic 主题字符串,支持通配符 (如 "devices/+/data")
   * @param qos QoS等级 (0, 1, 2)
   * @param onMessage 消息处理回调函数
   */
  async subscribeCustomTopic(
    topic: string,
    qos: 0 | 1 | 2 = 1,
    onMessage?: (data: any) => void
  ): Promise<void> {
    if (this._status.value !== 'connected') {
      throw new Error('MQTT未连接,无法订阅主题');
    }

    try {
      // TODO: 启用真实MQTT后取消下面的注释
      /*
      this.mqttClient.subscribe(topic, { qos }, (err) => {
        if (err) {
          console.error(`订阅主题失败: ${topic}`, err);
          throw err;
        }
        console.log(`已订阅主题: ${topic}, QoS: ${qos}`);
      });
      */
      
      this.subscribedTopics.add(topic);

      if (onMessage) {
        this.messageHandlers.set(topic, onMessage);
      }

      console.log(`已订阅主题: ${topic}, QoS: ${qos}`);
    } catch (error) {
      console.error(`订阅主题失败: ${topic}`, error);
      throw error;
    }
  }

  /**
   * 取消订阅自定义主题
   * @param topic 主题字符串
   */
  async unsubscribeCustomTopic(topic: string): Promise<void> {
    if (!this.mqttClient) {
      return;
    }

    try {
      // TODO: 启用真实MQTT后取消下面的注释
      // this.mqttClient?.unsubscribe(topic);
      
      this.subscribedTopics.delete(topic);
      this.messageHandlers.delete(topic);

      console.log(`已取消订阅主题: ${topic}`);
    } catch (error) {
      console.error(`取消订阅主题失败: ${topic}`, error);
      throw error;
    }
  }

  /**
   * 获取指定主题的设备数据
   */
  getDevicesByTopic(topic: string): DeviceData[] {
    return this._dataTopicDevices.get(topic) || [];
  }

  /**
   * 获取指定主题的告警消息
   */
  getAlarmsByTopic(topic: string): any[] {
    return this._alarmTopicMessages.get(topic) || [];
  }

  /**
   * 设置数据更新回调
   */
  onDataUpdate(callback: (topic: string, devices: DeviceData[]) => void): void {
    this.onDataUpdateCallback = callback;
  }

  /**
   * 设置告警更新回调
   */
  onAlarmUpdate(callback: (topic: string, alarms: any[]) => void): void {
    this.onAlarmUpdateCallback = callback;
  }

  /**
   * 处理接收到的MQTT消息
   */
  private handleMessage(topic: string, message: string): void {
    try {
      this._messageCount.value++;

      // 解析消息
      const messageData: MqttMessageData = JSON.parse(message);

      // 判断消息类型(根据主题)
      if (topic.includes('/data') || topic.includes('data')) {
        // 处理实时数据消息
        this.handleDataMessage(topic, messageData);
      } else if (topic.includes('/alarm') || topic.includes('alarm')) {
        // 处理告警消息
        this.handleAlarmMessage(topic, messageData);
      }

      // 调用自定义回调
      const handler = this.messageHandlers.get(topic);
      if (handler) {
        handler(messageData);
      }
    } catch (error) {
      console.error('处理MQTT消息失败:', error, '主题:', topic, '消息:', message);
    }
  }

  /**
   * 处理实时数据消息
   */
  private handleDataMessage(topic: string, messageData: MqttMessageData): void {
    try {
      // 解析 Result 字段中的设备数据
      const devices: DeviceData[] = JSON.parse(messageData.Result);

      // 更新设备数据
      this._dataTopicDevices.set(topic, devices);

      console.log(`收到实时数据消息 [${topic}]:`, devices.length, '个设备');

      // 通知外部组件数据已更新
      if (this.onDataUpdateCallback) {
        this.onDataUpdateCallback(topic, devices);
      }
    } catch (error) {
      console.error('解析实时数据消息失败:', error);
    }
  }

  /**
   * 处理告警消息
   */
  private handleAlarmMessage(topic: string, messageData: MqttMessageData): void {
    try {
      // 解析 Result 字段中的告警数据
      const alarms = JSON.parse(messageData.Result);

      // 更新告警数据
      const existingAlarms = this._alarmTopicMessages.get(topic) || [];
      this._alarmTopicMessages.set(topic, [...existingAlarms, ...alarms]);

      console.log(`收到告警消息 [${topic}]:`, alarms.length, '条告警');

      // 通知外部组件告警已更新
      if (this.onAlarmUpdateCallback) {
        this.onAlarmUpdateCallback(topic, alarms);
      }
    } catch (error) {
      console.error('解析告警消息失败:', error);
    }
  }

  /**
   * 重连调度
   */
  private scheduleReconnect(): void {
    if (this.currentReconnectAttempts >= this.maxReconnectAttempts) {
      console.error('达到最大重连次数，停止重连');
      return;
    }

    this.currentReconnectAttempts++;
    const delay = Math.min(1000 * Math.pow(2, this.currentReconnectAttempts), 30000);

    this.reconnectTimer = setTimeout(() => {
      console.log(`第 ${this.currentReconnectAttempts} 次重连尝试...`);
      if (this.currentConfig) {
        this.connect(this.currentConfig).catch(err => {
          console.error('重连失败:', err);
        });
      }
    }, delay);
  }

}

// 导出单例实例
export const fuxaMqttService = new FuxaMqttService();

export default fuxaMqttService;
