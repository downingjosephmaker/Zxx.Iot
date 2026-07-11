
// 测试设备列表接口
const testDeviceApi = async () => {
  if (!formData.value.config.deviceUrl) {
    ElMessage.warning('请先配置设备列表接口地址');
    return;
  }

  try {
    ElMessage.info('正在测试接口...');
    const response = await fetch(formData.value.config.deviceUrl, {
      method: 'GET',
      headers: formData.value.config.headers || {}
    });

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }

    const data = await response.json();

    // 验证数据格式
    if (!data.Result) {
      ElMessage.error('接口返回格式错误: 缺少 Result 字段');
      return;
    }

    const devices = JSON.parse(data.Result);
    ElMessage.success(`接口测试成功! 获取到 ${devices.length} 个设备`);
    console.log('设备列表:', devices);
  } catch (error) {
    ElMessage.error(`接口测试失败: ${error.message}`);
    console.error('测试错误:', error);
  }
};

<template>
  <el-dialog
    v-model="visible"
    title="数据集配置"
    width="900px"
    top="5vh"
    :close-on-click-modal="false"
    :close-on-press-escape="true"
    draggable
    align-center
  >
    <div class="dataset-dialog-content">
      <el-tabs v-model="activeTab" type="border-card">
        <!-- IoT设备点位配置(平台内置遥测通道,免手填URL) -->
        <el-tab-pane label="IoT设备点位" name="iot">
          <div class="dataset-config-section">
            <div class="config-section">
              <h4>基本信息</h4>
              <el-form :model="iotConfig" label-width="100px" size="small">
                <el-form-item label="数据集名称" required>
                  <el-input
                    v-model="iotConfig.name"
                    placeholder="请输入数据集名称"
                  />
                </el-form-item>
                <el-form-item label="描述">
                  <el-input
                    v-model="iotConfig.description"
                    type="textarea"
                    :rows="2"
                    placeholder="请输入数据集描述"
                  />
                </el-form-item>
              </el-form>
            </div>

            <div class="config-section">
              <h4>设备点位绑定</h4>
              <IotPointSelector :config="iotConfig" />
            </div>

            <el-alert
              type="info"
              :closable="false"
              show-icon
              title="实时模式：运行时以最新值铺底并经SignalR增量刷新；历史模式：按回看时长拉取遥测曲线(图表绑定用)"
            />
          </div>
        </el-tab-pane>

        <!-- API接口配置 -->
        <el-tab-pane label="API接口" name="api">
          <div class="dataset-config-section">
            <!-- 基本信息 -->
            <div class="config-section">
              <h4>基本信息</h4>
              <el-form :model="formData" label-width="100px" size="small">
                <el-row :gutter="16">
                  <el-col :span="12">
                    <el-form-item label="数据集名称" required>
                      <el-input
                        v-model="formData.name"
                        placeholder="请输入数据集名称"
                      />
                    </el-form-item>
                  </el-col>
                  <el-col :span="12">
                    <el-form-item label="数据集ID">
                      <el-input
                        v-model="formData.id"
                        placeholder="自动生成"
                        disabled
                      />
                    </el-form-item>
                  </el-col>
                </el-row>
                <el-form-item label="描述">
                  <el-input
                    v-model="formData.description"
                    type="textarea"
                    :rows="2"
                    placeholder="请输入数据集描述"
                  />
                </el-form-item>
              </el-form>
            </div>

            <!-- 接口配置 -->
            <div class="config-section">
              <h4>接口配置</h4>
              <el-form
                :model="formData.config"
                label-width="100px"
                size="small"
              >
                <el-row :gutter="16">
                  <el-col :span="8">
                    <el-form-item label="请求方法" required>
                      <el-select
                        v-model="formData.config.method"
                        style="width: 100%"
                      >
                        <el-option label="GET" value="GET" />
                        <el-option label="POST" value="POST" />
                        <el-option label="PUT" value="PUT" />
                        <el-option label="DELETE" value="DELETE" />
                      </el-select>
                    </el-form-item>
                  </el-col>
                  <el-col :span="8">
                    <el-form-item label="超时时间(ms)">
                      <el-input-number
                        v-model="formData.config.timeout"
                        :min="1000"
                        :max="300000"
                        :step="1000"
                        style="width: 100%"
                      />
                    </el-form-item>
                  </el-col>
                  <el-col :span="8">
                    <el-form-item label="刷新间隔(ms)">
                      <el-input-number
                        v-model="formData.config.interval"
                        :min="1000"
                        :max="3600000"
                        :step="1000"
                        style="width: 100%"
                      />
                    </el-form-item>
                  </el-col>
                </el-row>
                <el-form-item label="接口地址" required>
                  <el-input
                    v-model="formData.config.url"
                    placeholder="http://api.example.com/devices/data"
                  />
                </el-form-item>
                <el-form-item label="设备列表接口" required>
                  <el-input
                    v-model="formData.config.deviceUrl"
                    placeholder="http://api.example.com/devices/list (返回 ScadaDevice 数组格式)"
                  >
                    <template #append>
                      <el-button @click="testDeviceApi">
                        <el-icon><Check /></el-icon>
                        测试
                      </el-button>
                    </template>
                  </el-input>
                  <div style="margin-top: 4px; font-size: 12px; color: #909399;">
                    返回格式: {Result: "[ScadaDevice数组]", Timestamp, Status, Message, Total}
                  </div>
                </el-form-item>
              </el-form>
            </div>

            <!-- Token认证配置 -->
            <div class="config-section">
              <h4>Token认证配置</h4>
              <el-form :model="formData.config.auth" label-width="120px" size="small">
                <el-form-item label="认证方式">
                  <el-select
                    v-model="formData.config.auth.type"
                    style="width: 100%"
                    @change="onAuthTypeChange"
                  >
                    <el-option label="无认证" value="none" />
                    <el-option label="Bearer Token" value="bearer" />
                    <el-option label="自定义Token" value="customtoken" />
                    <el-option label="API Key" value="apikey" />
                    <el-option label="Basic Auth" value="basic" />
                    <el-option label="Custom Header" value="custom" />
                  </el-select>
                </el-form-item>

                <!-- Bearer Token配置 -->
                <template v-if="formData.config.auth.type === 'bearer'">
                  <el-form-item label="Token获取方式">
                    <el-radio-group v-model="formData.config.auth.tokenSource">
                      <el-radio label="manual">手动输入</el-radio>
                      <el-radio label="api">接口获取</el-radio>
                    </el-radio-group>
                  </el-form-item>

                  <!-- 手动输入Token -->
                  <template v-if="formData.config.auth.tokenSource === 'manual'">
                    <el-form-item label="Access Token">
                      <el-input
                        v-model="formData.config.auth.token"
                        type="password"
                        placeholder="请输入Bearer Token"
                        show-password
                      />
                    </el-form-item>
                  </template>

                  <!-- 接口获取Token -->
                  <template v-if="formData.config.auth.tokenSource === 'api'">
                    <el-form-item label="认证接口">
                      <el-input
                        v-model="formData.config.auth.tokenUrl"
                        placeholder="https://api.example.com/auth/token"
                      />
                    </el-form-item>
                    <el-form-item label="请求方法">
                      <el-select v-model="formData.config.auth.tokenMethod" style="width: 100%">
                        <el-option label="POST" value="POST" />
                        <el-option label="GET" value="GET" />
                      </el-select>
                    </el-form-item>
                    <el-form-item label="请求参数">
                      <el-input
                        v-model="formData.config.auth.tokenParams"
                        type="textarea"
                        :rows="3"
                        placeholder='{"username": "your_username", "password": "your_password"}'
                      />
                    </el-form-item>
                    <el-form-item label="Token路径">
                      <el-input
                        v-model="formData.config.auth.tokenPath"
                        placeholder="data.access_token (JSON路径)"
                      />
                    </el-form-item>
                    <el-form-item label="刷新间隔(分钟)">
                      <el-input-number
                        v-model="formData.config.auth.refreshInterval"
                        :min="5"
                        :max="1440"
                        style="width: 100%"
                      />
                    </el-form-item>
                    <el-form-item>
                      <el-button
                        type="success"
                        size="small"
                        @click="fetchToken"
                        :loading="fetchingToken"
                      >
                        <el-icon><Refresh /></el-icon>
                        获取Token
                      </el-button>
                      <span v-if="formData.config.auth.lastTokenFetch" class="token-status">
                        上次获取: {{ formData.config.auth.lastTokenFetch }}
                      </span>
                    </el-form-item>
                  </template>
                </template>

                <!-- API Key配置 -->
                <template v-if="formData.config.auth.type === 'apikey'">
                  <el-form-item label="API Key名称">
                    <el-input v-model="formData.config.auth.keyName" placeholder="X-API-Key" />
                  </el-form-item>
                  <el-form-item label="API Key值">
                    <el-input
                      v-model="formData.config.auth.keyValue"
                      type="password"
                      placeholder="请输入API Key"
                      show-password
                    />
                  </el-form-item>
                  <el-form-item label="传递方式">
                    <el-select v-model="formData.config.auth.keyLocation" style="width: 100%">
                      <el-option label="Header" value="header" />
                      <el-option label="Query参数" value="query" />
                    </el-select>
                  </el-form-item>
                </template>

                <!-- Basic Auth配置 -->
                <template v-if="formData.config.auth.type === 'basic'">
                  <el-form-item label="用户名">
                    <el-input v-model="formData.config.auth.username" />
                  </el-form-item>
                  <el-form-item label="密码">
                    <el-input
                      v-model="formData.config.auth.password"
                      type="password"
                      show-password
                    />
                  </el-form-item>
                </template>

                <!-- 自定义Token配置 -->
                <template v-if="formData.config.auth.type === 'customtoken'">
                  <el-form-item label="Token获取方式">
                    <el-radio-group v-model="formData.config.auth.customTokenSource">
                      <el-radio label="manual">手动输入</el-radio>
                      <el-radio label="api">接口获取</el-radio>
                    </el-radio-group>
                  </el-form-item>

                  <!-- Token格式配置 -->
                  <el-form-item label="Token格式">
                    <el-input
                      v-model="formData.config.auth.customTokenFormat"
                      placeholder="例如: Bearer {token} 或 Token {token} 或 {token}"
                    >
                      <template #prepend>格式模板</template>
                    </el-input>
                    <div class="token-format-help">
                      <small>使用 {token} 作为占位符，例如："Bearer {token}"、"Token {token}"、"X-Auth-Token: {token}"</small>
                    </div>
                  </el-form-item>

                  <el-form-item label="Header名称">
                    <el-input
                      v-model="formData.config.auth.customTokenHeader"
                      placeholder="Authorization"
                    />
                  </el-form-item>

                  <!-- 手动输入Token -->
                  <template v-if="formData.config.auth.customTokenSource === 'manual'">
                    <el-form-item label="Token值">
                      <el-input
                        v-model="formData.config.auth.customToken"
                        type="password"
                        placeholder="请输入Token值"
                        show-password
                      />
                    </el-form-item>
                  </template>

                  <!-- 接口获取Token -->
                  <template v-if="formData.config.auth.customTokenSource === 'api'">
                    <el-form-item label="认证接口">
                      <el-input
                        v-model="formData.config.auth.customTokenUrl"
                        placeholder="https://api.example.com/auth/token"
                      />
                    </el-form-item>
                    <el-form-item label="请求方法">
                      <el-select v-model="formData.config.auth.customTokenMethod" style="width: 100%">
                        <el-option label="POST" value="POST" />
                        <el-option label="GET" value="GET" />
                        <el-option label="PUT" value="PUT" />
                      </el-select>
                    </el-form-item>
                    <el-form-item label="请求参数">
                      <el-input
                        v-model="formData.config.auth.customTokenParams"
                        type="textarea"
                        :rows="3"
                        placeholder='{"username": "your_username", "password": "your_password"}'
                      />
                    </el-form-item>
                    <el-form-item label="Token路径">
                      <el-input
                        v-model="formData.config.auth.customTokenPath"
                        placeholder="data.access_token 或 token 或 result.authToken"
                      />
                    </el-form-item>
                    <el-form-item label="刷新间隔(分钟)">
                      <el-input-number
                        v-model="formData.config.auth.customRefreshInterval"
                        :min="5"
                        :max="1440"
                        style="width: 100%"
                      />
                    </el-form-item>
                    <el-form-item>
                      <el-button
                        type="success"
                        size="small"
                        @click="fetchCustomToken"
                        :loading="fetchingCustomToken"
                      >
                        <el-icon><Refresh /></el-icon>
                        获取自定义Token
                      </el-button>
                      <span v-if="formData.config.auth.lastCustomTokenFetch" class="token-status">
                        上次获取: {{ formData.config.auth.lastCustomTokenFetch }}
                      </span>
                    </el-form-item>
                  </template>

                  <!-- Token预览 -->
                  <el-form-item label="Token预览">
                    <el-input
                      :model-value="getCustomTokenPreview()"
                      readonly
                      placeholder="配置完成后这里将显示最终的Token格式"
                    >
                      <template #prepend>{{ formData.config.auth.customTokenHeader || 'Authorization' }}</template>
                    </el-input>
                  </el-form-item>
                </template>

                <!-- 自定义Header -->
                <template v-if="formData.config.auth.type === 'custom'">
                  <el-form-item label="Header名称">
                    <el-input v-model="formData.config.auth.headerName" placeholder="Authorization" />
                  </el-form-item>
                  <el-form-item label="Header值">
                    <el-input
                      v-model="formData.config.auth.headerValue"
                      type="password"
                      placeholder="请输入Header值"
                      show-password
                    />
                  </el-form-item>
                </template>
              </el-form>
            </div>

            <!-- 请求头配置 -->
            <div class="config-section">
              <h4>请求头配置</h4>
              <div class="headers-config">
                <div
                  v-for="(value, key, index) in formData.config.headers"
                  :key="index"
                  class="header-item"
                >
                  <el-input
                    :model-value="key"
                    placeholder="Header Name"
                    style="width: 40%; margin-right: 10px"
                    @input="updateHeaderKey(key, $event)"
                  />
                  <el-input
                    v-model="formData.config.headers[key]"
                    placeholder="Header Value"
                    style="width: 40%; margin-right: 10px"
                  />
                  <el-button
                    type="danger"
                    size="small"
                    @click="removeHeader(key)"
                  >
                    删除
                  </el-button>
                </div>
                <el-button type="primary" size="small" @click="addHeader">
                  添加请求头
                </el-button>
                <div class="auth-headers-info" v-if="formData.config.auth.type !== 'none'">
                  <el-alert
                    title="认证头部将自动添加到请求中"
                    type="info"
                    :closable="false"
                    show-icon
                  />
                </div>
              </div>
            </div>

            <!-- 请求体配置 -->
            <div v-if="formData.config.method !== 'GET'" class="config-section">
              <h4>请求体配置</h4>
              <el-form label-width="100px" size="small">
                <el-form-item label="内容类型">
                  <el-select
                    v-model="formData.config.contentType"
                    style="width: 100%"
                  >
                    <el-option
                      label="application/json"
                      value="application/json"
                    />
                    <el-option
                      label="application/x-www-form-urlencoded"
                      value="application/x-www-form-urlencoded"
                    />
                    <el-option label="text/plain" value="text/plain" />
                  </el-select>
                </el-form-item>
                <el-form-item label="请求体">
                  <el-input
                    v-model="formData.config.body"
                    type="textarea"
                    :rows="6"
                    placeholder="请输入请求体内容，支持JSON格式"
                  />
                </el-form-item>
              </el-form>
            </div>

            <!-- 测试区域 -->
            <div class="config-section">
              <h4>接口测试</h4>
              <div class="test-area">
                <el-button
                  type="success"
                  @click="testDataset"
                  :loading="testing"
                >
                  <el-icon><VideoPlay /></el-icon>
                  测试接口
                </el-button>
                <div v-if="formData.testResult" class="test-result">
                  <div class="result-header">
                    <span>测试结果</span>
                    <span class="test-time">{{ formData.lastTested }}</span>
                  </div>
                  <el-input
                    :model-value="JSON.stringify(formData.testResult, null, 2)"
                    type="textarea"
                    :rows="10"
                    readonly
                  />
                </div>
              </div>
            </div>
          </div>
        </el-tab-pane>

        <!-- MQTT配置 -->
        <el-tab-pane label="MQTT订阅" name="mqtt">
          <div class="dataset-config-section">
            <!-- MQTT连接配置 -->
            <div class="config-section">
              <h4>连接配置</h4>
              <el-form :model="mqttConfig" label-width="100px" size="small">
                <el-row :gutter="16">
                  <el-col :span="12">
                    <el-form-item label="MQTT地址">
                      <el-input
                        v-model="mqttConfig.host"
                        placeholder="mqtt://localhost:1883"
                      />
                    </el-form-item>
                  </el-col>
                  <el-col :span="12">
                    <el-form-item label="客户端ID">
                      <el-input
                        v-model="mqttConfig.clientId"
                        placeholder="自动生成"
                      />
                    </el-form-item>
                  </el-col>
                </el-row>
                <el-row :gutter="16">
                  <el-col :span="12">
                    <el-form-item label="用户名">
                      <el-input
                        v-model="mqttConfig.username"
                        placeholder="可选"
                      />
                    </el-form-item>
                  </el-col>
                  <el-col :span="12">
                    <el-form-item label="密码">
                      <el-input
                        v-model="mqttConfig.password"
                        type="password"
                        placeholder="可选"
                      />
                    </el-form-item>
                  </el-col>
                </el-row>
              </el-form>
            </div>

            <!-- 订阅配置 -->
            <div class="config-section">
              <h4>订阅配置</h4>
              <el-form :model="mqttConfig" label-width="100px" size="small">
                <el-form-item label="实时数据主题" required>
                  <el-input
                    v-model="mqttConfig.dataTopic"
                    placeholder="scada/devices/data"
                  >
                    <template #append>
                      <el-tooltip content="接收设备实时数据的MQTT主题" placement="top">
                        <el-icon><QuestionFilled /></el-icon>
                      </el-tooltip>
                    </template>
                  </el-input>
                </el-form-item>
                <el-form-item label="告警主题">
                  <el-input
                    v-model="mqttConfig.alarmTopic"
                    placeholder="scada/devices/alarm"
                  >
                    <template #append>
                      <el-tooltip content="接收设备告警消息的MQTT主题(可选)" placement="top">
                        <el-icon><QuestionFilled /></el-icon>
                      </el-tooltip>
                    </template>
                  </el-input>
                </el-form-item>
                <el-form-item label="QoS等级">
                  <el-select v-model="mqttConfig.qos" style="width: 100%">
                    <el-option label="0 - 最多一次" value="0" />
                    <el-option label="1 - 至少一次" value="1" />
                    <el-option label="2 - 仅一次" value="2" />
                  </el-select>
                </el-form-item>
              </el-form>
            </div>
          </div>
        </el-tab-pane>



        <!-- 静态数据配置 -->
        <el-tab-pane label="静态数据" name="static">
          <div class="dataset-config-section">
            <div class="config-section">
              <h4>静态数据配置</h4>
              <div class="static-data-editor">
                <div class="data-format-selector">
                  <el-form label-width="100px" size="small">
                    <el-form-item label="数据格式">
                      <el-radio-group v-model="staticDataFormat">
                        <el-radio-button label="json">JSON格式</el-radio-button>
                        <el-radio-button label="table"
                          >表格格式</el-radio-button
                        >
                      </el-radio-group>
                    </el-form-item>
                  </el-form>
                </div>

                <!-- JSON格式输入 -->
                <div v-if="staticDataFormat === 'json'" class="json-editor">
                  <el-input
                    v-model="staticJsonDataForEditor"
                    type="textarea"
                    :rows="12"
                    :placeholder="staticJsonPlaceholder"
                  />
                  <div style="margin-top: 12px; display: flex; gap: 8px;">
                    <el-button type="info" size="small" @click="formatJson">
                      格式化JSON
                    </el-button>
                    <el-button type="success" size="small" @click="validateJson">
                      验证JSON
                    </el-button>
                  </div>
                </div>

                <!-- 表格格式输入 -->
                <div v-else class="table-editor">
                  <div class="table-controls">
                    <el-button
                      type="primary"
                      size="small"
                      @click="addStaticRow"
                    >
                      添加行
                    </el-button>
                    <el-button
                      type="warning"
                      size="small"
                      @click="clearStaticTable"
                    >
                      清空表格
                    </el-button>
                  </div>
                  <el-table
                    :data="staticTableData"
                    border
                    size="small"
                    style="width: 100%; margin-top: 12px"
                    max-height="400"
                  >
                    <el-table-column
                      v-for="column in staticTableColumns"
                      :key="column.prop"
                      :prop="column.prop"
                      :label="column.label"
                      :width="column.width"
                      :min-width="column.minWidth"
                    >
                      <template #default="{ row }">
                        <!-- 设备状态: 2在线/1掉电/0离线 -->
                        <el-tag
                          v-if="column.prop === 'DeviceState'"
                          :type="row.DeviceState === 2 ? 'success' : row.DeviceState === 1 ? 'warning' : 'info'"
                          size="small"
                        >
                          {{ row.DeviceState === 2 ? '在线' : row.DeviceState === 1 ? '掉电' : '离线' }}
                        </el-tag>

                        <!-- 告警状态: 1告警/0正常 -->
                        <el-tag
                          v-else-if="column.prop === 'DeviceAlarm'"
                          :type="row.DeviceAlarm === 1 ? 'danger' : 'success'"
                          size="small"
                        >
                          {{ row.DeviceAlarm === 1 ? '告警' : '正常' }}
                        </el-tag>

                        <!-- 开关状态: 0关/1开 -->
                        <el-tag
                          v-else-if="column.prop === 'DeviceSwitch'"
                          :type="row.DeviceSwitch === 1 ? 'success' : 'info'"
                          size="small"
                        >
                          {{ row.DeviceSwitch === 1 ? '开' : '关' }}
                        </el-tag>

                        <!-- 其他列正常显示 -->
                        <span v-else>{{ row[column.prop] }}</span>
                      </template>
                    </el-table-column>
                    <el-table-column label="操作" width="150" fixed="right">
                      <template #default="{ row, $index }">
                        <el-button
                          type="primary"
                          size="small"
                          @click="showDeviceParams(row)"
                        >
                          参数详情
                        </el-button>
                        <el-button
                          type="danger"
                          size="small"
                          @click="removeStaticRow($index)"
                        >
                          删除
                        </el-button>
                      </template>
                    </el-table-column>
                  </el-table>
                </div>

                <!-- 数据预览 -->
                <div class="data-preview">
                  <h5>数据预览</h5>
                  <el-input
                    :model-value="getPreviewData()"
                    type="textarea"
                    :rows="8"
                    readonly
                  />
                </div>
              </div>
            </div>
          </div>
        </el-tab-pane>
      </el-tabs>
    </div>

    <template #footer>
      <div class="dialog-footer">
        <el-button @click="handleCancel">取消</el-button>
        <el-button type="success" @click="testDataset" :loading="testing">
          <el-icon><VideoPlay /></el-icon>
          测试连接
        </el-button>
        <el-button type="primary" @click="saveConfig">
          <el-icon><Check /></el-icon>
          保存配置
        </el-button>
      </div>
    </template>
  </el-dialog>

  <!-- 参数详情弹窗 -->
  <el-dialog
    v-model="paramsDialogVisible"
    :title="`${selectedDevice?.DeviceName || ''} - 参数列表`"
    width="900px"
    append-to-body
  >
    <div v-if="selectedDevice">
      <!-- 设备状态栏 -->
      <div style="margin-bottom: 16px; display: flex; gap: 12px; align-items: center;">
        <el-tag :type="selectedDevice.DeviceState === 2 ? 'success' : selectedDevice.DeviceState === 1 ? 'warning' : 'info'" size="large">
          {{ selectedDevice.DeviceState === 2 ? '在线' : selectedDevice.DeviceState === 1 ? '掉电' : '离线' }}
        </el-tag>
        <el-tag :type="selectedDevice.DeviceAlarm === 1 ? 'danger' : 'success'" size="large">
          {{ selectedDevice.DeviceAlarm === 1 ? '告警' : '正常' }}
        </el-tag>
        <el-tag :type="selectedDevice.DeviceSwitch === 1 ? 'success' : 'info'" size="large">
          {{ selectedDevice.DeviceSwitch === 1 ? '开启' : '关闭' }}
        </el-tag>
        <span style="color: #909399; font-size: 14px; margin-left: auto;">
          更新时间: {{ selectedDevice.LastOnlineTime || '-' }}
        </span>
      </div>

      <!-- 参数表格 -->
      <el-table
        :data="selectedDevice.DeviceParams || []"
        border
        stripe
        size="default"
        max-height="500"
      >
        <el-table-column prop="ParamCode" label="参数编码" width="120" />
        <el-table-column prop="ParamName" label="参数名称" min-width="140" />
        <el-table-column prop="ParamValue" label="当前值" width="100" align="right">
          <template #default="{ row }">
            <span style="font-weight: 600; color: #409eff;">{{ row.ParamValue }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="ParamLastValue" label="上次值" width="100" align="right">
          <template #default="{ row }">
            <span style="color: #909399;">{{ row.ParamLastValue || '-' }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="ValueUnit" label="单位" width="80" align="center" />
        <el-table-column prop="CollectTime" label="采集时间" width="160" />
        <el-table-column prop="IsAlarm" label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="row.IsAlarm === 1 ? 'danger' : 'success'" size="small">
              {{ row.IsAlarm === 1 ? '告警' : '正常' }}
            </el-tag>
          </template>
        </el-table-column>
      </el-table>
    </div>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, onBeforeUnmount } from "vue";
import { ElMessage } from "element-plus";
import { VideoPlay, Check, Refresh } from "@element-plus/icons-vue";
import IotPointSelector from "./IotPointSelector.vue";
import { getDeviceLatest } from "@/api/iot/monitor";

// 组件属性
interface Props {
  visible: boolean;
  dataset?: any;
}

const props = withDefaults(defineProps<Props>(), {
  visible: false,
  dataset: null
});

// 组件事件
const emit = defineEmits<{
  "update:visible": [visible: boolean];
  "save-config": [config: any];
  "test-dataset": [config: any];
}>();

// 响应式状态(默认停在IoT设备点位——平台内置通道免手填URL)
const activeTab = ref("iot");
const testing = ref(false);
const fetchingToken = ref(false);
const fetchingCustomToken = ref(false);
const tokenRefreshTimer = ref(null);
const customTokenRefreshTimer = ref(null);

// 数据集配置表单
const formData = reactive({
  id: "",
  name: "",
  description: "",
  type: "api",
  config: {
    url: "",
  deviceUrl: "",
  method: "GET",
    headers: {},
    params: {},
    body: "",
    timeout: 30000,
    interval: 5000,
    contentType: "application/json",
    auth: {
      type: "none",
      // Bearer Token
      tokenSource: "manual",
      token: "",
      tokenUrl: "",
      tokenMethod: "POST",
      tokenParams: "",
      tokenPath: "access_token",
      refreshInterval: 60,
      lastTokenFetch: null,
      // Custom Token
      customTokenSource: "manual",
      customToken: "",
      customTokenFormat: "Bearer {token}",
      customTokenHeader: "Authorization",
      customTokenUrl: "",
      customTokenMethod: "POST",
      customTokenParams: "",
      customTokenPath: "access_token",
      customRefreshInterval: 60,
      lastCustomTokenFetch: null,
      // API Key
      keyName: "X-API-Key",
      keyValue: "",
      keyLocation: "header",
      // Basic Auth
      username: "",
      password: "",
      // Custom Header
      headerName: "Authorization",
      headerValue: ""
    }
  },
  testResult: null,
  lastTested: null
});

// IoT设备点位数据集配置(结构=core/DatasetRuntime.IotDatasetConfig)
const iotConfig = reactive({
  name: "",
  description: "",
  deviceId: null as number | null,
  deviceName: "",
  deviceTypeCode: "",
  mode: "realtime" as "realtime" | "history",
  points: [] as { ParamCode: string; ParamName?: string; ValueUnit?: string }[],
  historyHours: 24,
  historyMode: "auto" as "auto" | "raw" | "hour"
});

// MQTT配置
const mqttConfig = reactive({
  host: "mqtt://localhost:1883",
  clientId: "",
  username: "",
  password: "",
  topic: "",
  qos: "1"
});

// WebSocket配置
const wsConfig = reactive({
  url: "",
  protocol: "",
  heartbeatInterval: 30
});

// 数据库配置
const dbConfig = reactive({
  type: "mysql",
  host: "localhost",
  port: 3306,
  database: "",
  username: "",
  password: "",
  query: "",
  queryInterval: 60
});

// 静态数据配置
const staticDataFormat = ref("json");
const staticJsonData = ref(`{
  "Result": "[{\\"DeviceId\\":\\"120025\\",\\"DeviceName\\":\\"1号配电间西侧叉车充电桩\\",\\"DeviceTypeCode\\":\\"Modbus_NW\\",\\"DeviceTypeFullCode\\":\\"|zndb|Modbus_NW|\\",\\"DeviceGuid\\":\\"172.15.30.35_502_35\\",\\"LastOnlineTime\\":\\"2025-09-15 09:07:06\\",\\"DeviceState\\":2,\\"DeviceAlarm\\":0,\\"DeviceSwitch\\":0,\\"DeviceFullCode\\":\\"|169|228|120025|\\",\\"DeviceParams\\":[{\\"ParamCode\\":\\"Ia\\",\\"ParamName\\":\\"A相电流\\",\\"ValueUnit\\":\\"A\\",\\"CollectTime\\":\\"2025-09-15 09:07:06\\",\\"ParamLastValue\\":\\"7.200\\",\\"ParamValue\\":\\"7.600\\",\\"IsAlarm\\":0},{\\"ParamCode\\":\\"Ib\\",\\"ParamName\\":\\"B相电流\\",\\"ValueUnit\\":\\"A\\",\\"CollectTime\\":\\"2025-09-15 09:07:06\\",\\"ParamLastValue\\":\\"7.360\\",\\"ParamValue\\":\\"7.360\\",\\"IsAlarm\\":0},{\\"ParamCode\\":\\"Ic\\",\\"ParamName\\":\\"C相电流\\",\\"ValueUnit\\":\\"A\\",\\"CollectTime\\":\\"2025-09-15 09:07:06\\",\\"ParamLastValue\\":\\"7.328\\",\\"ParamValue\\":\\"7.184\\",\\"IsAlarm\\":0}]},{\\"DeviceId\\":\\"90036\\",\\"DeviceName\\":\\"堆高车DG0970CD0372\\",\\"DeviceTypeCode\\":\\"liuji\\",\\"DeviceTypeFullCode\\":\\"|zhcl|liuji|\\",\\"DeviceGuid\\":\\"DG0970CD0372\\",\\"LastOnlineTime\\":\\"2025-09-15 09:10:00\\",\\"DeviceState\\":2,\\"DeviceAlarm\\":0,\\"DeviceSwitch\\":0,\\"DeviceFullCode\\":\\"|90036|\\",\\"DeviceParams\\":[{\\"ParamCode\\":\\"energy\\",\\"ParamName\\":\\"总电耗\\",\\"ValueUnit\\":\\"kWh\\",\\"CollectTime\\":null,\\"ParamLastValue\\":null,\\"ParamValue\\":\\"128844\\",\\"IsAlarm\\":0}]}]",
  "Timestamp": "2025-11-14 09:29:54",
  "Status": true,
  "Message": "信息处理成功。",
  "Total": 317
}`);
const staticTableData = computed(() => {
  try {
    const data = JSON.parse(staticJsonData.value);
    if (data.Result && typeof data.Result === 'string') {
      return JSON.parse(data.Result);
    }
    return [];
  } catch (error) {
    console.error('解析静态数据失败:', error);
    return [];
  }
});
// 用于 JSON 编辑器显示的数据 (显示完整的 staticJsonData)
const staticJsonDataForEditor = computed({
  get: () => {
    try {
      // 返回格式化后的完整 staticJsonData
      const data = JSON.parse(staticJsonData.value);
      return JSON.stringify(data, null, 2);
    } catch (error) {
      return staticJsonData.value;
    }
  },
  set: (value) => {
    try {
      // 解析用户输入的完整数据
      const data = JSON.parse(value);

      // 验证数据格式 (必须包含 Result 字段)
      if (!data.Result) {
        ElMessage.error('数据格式错误: 缺少 Result 字段');
        return;
      }

      // 更新 staticJsonData
      staticJsonData.value = JSON.stringify(data, null, 2);
      ElMessage.success('JSON数据已更新');
    } catch (error) {
      console.error('JSON 解析失败:', error);
      ElMessage.error(`JSON格式错误: ${error.message}`);
    }
  }
});

// JSON 输入框的 placeholder (从 staticJsonData 生成示例)
const staticJsonPlaceholder = computed(() => {
  try {
    const data = JSON.parse(staticJsonData.value);
    // 生成简化的示例格式
    const example = {
      Result: "[设备数组JSON字符串]",
      Timestamp: data.Timestamp || "2025-01-01 00:00:00",
      Status: data.Status !== undefined ? data.Status : true,
      Message: data.Message || "信息处理成功。",
      Total: data.Total || 0
    };
    return JSON.stringify(example, null, 2);
  } catch (error) {
    return '{\n  "Result": "[...]",\n  "Timestamp": "2025-01-01 00:00:00",\n  "Status": true,\n  "Message": "信息处理成功。",\n  "Total": 0\n}';
  }
});

const staticTableColumns = ref([
  { prop: "DeviceName", label: "设备名称", minWidth: 150 },
  { prop: "DeviceTypeName", label: "设备类型", minWidth: 120 },
  { prop: "DeviceGuid", label: "设备编号", minWidth: 150 },
  { prop: "LastOnlineTime", label: "最后在线时间", minWidth: 160 },
  { prop: "DeviceState", label: "设备状态", width: 100 },
  { prop: "DeviceAlarm", label: "告警状态", width: 100 },
  { prop: "DeviceSwitch", label: "开关状态", width: 100 }
]);

// 参数详情弹窗状态
const paramsDialogVisible = ref(false);
const selectedDevice = ref(null as any);

// 显示设备参数详情
const showDeviceParams = (device: any) => {
  selectedDevice.value = device;
  paramsDialogVisible.value = true;
};

// 计算属性
const visible = computed({
  get: () => props.visible,
  set: value => emit("update:visible", value)
});

// 方法
const handleCancel = () => {
  visible.value = false;
};

const saveConfig = () => {
  // 根据当前选中的tab收集配置
  let config = null;

  switch (activeTab.value) {
    case "iot":
      if (!iotConfig.name) {
        ElMessage.warning("请输入数据集名称");
        return;
      }
      if (!iotConfig.deviceId || !iotConfig.points.length) {
        ElMessage.warning("请选择绑定设备与至少一个点位");
        return;
      }
      config = {
        type: "iot",
        id: props.dataset?.id || "",
        ...JSON.parse(JSON.stringify(iotConfig))
      };
      break;
    case "api":
      // 合并认证头部到请求头中
      const authHeaders = buildAuthHeaders();
      const mergedHeaders = { ...formData.config.headers, ...authHeaders };
      config = {
        type: "api",
        ...formData,
        config: {
          ...formData.config,
          headers: mergedHeaders
        }
      };
      break;
    case "mqtt":
      config = {
        type: "mqtt",
        ...mqttConfig
      };
      break;
    case "websocket":
      config = {
        type: "websocket",
        ...wsConfig
      };
      break;
    case "database":
      config = {
        type: "database",
        ...dbConfig
      };
      break;
    case "static":
      config = {
        type: "static",
        format: staticDataFormat.value,
        data:
          staticDataFormat.value === "json"
            ? staticJsonData.value
            : staticTableData.value
      };
      break;
  }

  emit("save-config", config);
  visible.value = false;
  ElMessage.success("数据集配置已保存");
};

const testDataset = async () => {
  // IoT点位走平台真实接口测试，不进入下方模拟流程
  if (activeTab.value === "iot") {
    if (!iotConfig.deviceId) {
      ElMessage.warning("请先选择绑定设备");
      return;
    }
    testing.value = true;
    try {
      const data = await getDeviceLatest(iotConfig.deviceId);
      if (!data.Status) throw new Error(data.Message || "查询失败");
      const points = JSON.parse(data.Result) as { ParamCode: string }[];
      const codes = new Set(iotConfig.points.map(p => p.ParamCode));
      const hit = codes.size
        ? points.filter(p => codes.has(p.ParamCode)).length
        : points.length;
      ElMessage.success(
        `连接成功：设备共${points.length}个点位有最新值，命中已勾选点位${hit}个`
      );
    } catch (error) {
      ElMessage.error("测试失败: " + (error as Error).message);
    } finally {
      testing.value = false;
    }
    return;
  }

  testing.value = true;
  try {
    let testConfig = null;

    switch (activeTab.value) {
      case "api":
        // 合并认证头部到请求头中
        const authHeaders = buildAuthHeaders();
        const mergedHeaders = { ...formData.config.headers, ...authHeaders };
        testConfig = {
          ...formData,
          config: {
            ...formData.config,
            headers: mergedHeaders
          }
        };
        break;
      case "mqtt":
        testConfig = mqttConfig;
        break;
      case "websocket":
        testConfig = wsConfig;
        break;
      case "database":
        testConfig = dbConfig;
        break;
      case "static":
        testConfig = {
          format: staticDataFormat.value,
          data:
            staticDataFormat.value === "json"
              ? staticJsonData.value
              : staticTableData.value
        };
        break;
    }

    emit("test-dataset", testConfig);

    // 模拟测试结果
    setTimeout(() => {
      formData.testResult = {
        status: "success",
        data: { message: "测试成功", timestamp: new Date().toISOString() }
      };
      formData.lastTested = new Date().toLocaleString();
      testing.value = false;
      ElMessage.success("数据集测试成功");
    }, 2000);
  } catch (error) {
    testing.value = false;
    ElMessage.error("数据集测试失败: " + (error as Error).message);
  }
};

// 请求头管理
const addHeader = () => {
  const newKey = `Header-${Object.keys(formData.config.headers).length + 1}`;
  formData.config.headers[newKey] = "";
};

const removeHeader = (key: string) => {
  delete formData.config.headers[key];
};

const updateHeaderKey = (oldKey: string, newKey: string) => {
  if (oldKey !== newKey && newKey) {
    const value = formData.config.headers[oldKey];
    delete formData.config.headers[oldKey];
    formData.config.headers[newKey] = value;
  }
};

// 静态数据管理
const formatJson = () => {
  try {
    const parsed = JSON.parse(staticJsonData.value);
    staticJsonData.value = JSON.stringify(parsed, null, 2);
    ElMessage.success("JSON格式化成功");
  } catch (error) {
    ElMessage.error("JSON格式错误");
  }
};

const validateJson = () => {
  try {
    JSON.parse(staticJsonData.value);
    ElMessage.success("JSON验证通过");
  } catch (error) {
    ElMessage.error("JSON格式错误");
  }
};

const addStaticRow = () => {
  try {
    const data = JSON.parse(staticJsonData.value);
    let devices = [];

    if (data.Result && typeof data.Result === 'string') {
      devices = JSON.parse(data.Result);
    }

    const newRow = {
      DeviceId: `${Date.now()}`,
      DeviceName: "新设备",
      DeviceTypeName: "传感器",
      DeviceTypeCode: "sensor",
      DeviceGuid: `DEV_${Date.now()}`,
      LastOnlineTime: new Date().toISOString().slice(0, 19).replace('T', ' '),
      DeviceState: 0,
      DeviceAlarm: 0,
      DeviceSwitch: 0,
      DeviceParams: []
    };

    devices.push(newRow);
    data.Result = JSON.stringify(devices);
    data.Total = devices.length;
    data.Timestamp = new Date().toISOString().slice(0, 19).replace('T', ' ');

    staticJsonData.value = JSON.stringify(data, null, 2);
    ElMessage.success("已添加新设备");
  } catch (error) {
    ElMessage.error(`添加设备失败: ${error.message}`);
  }
};

const removeStaticRow = (index: number) => {
  try {
    const data = JSON.parse(staticJsonData.value);

    if (data.Result && typeof data.Result === 'string') {
      const devices = JSON.parse(data.Result);
      devices.splice(index, 1);

      data.Result = JSON.stringify(devices);
      data.Total = devices.length;
      data.Timestamp = new Date().toISOString().slice(0, 19).replace('T', ' ');

      staticJsonData.value = JSON.stringify(data, null, 2);
      ElMessage.success("已删除设备");
    }
  } catch (error) {
    ElMessage.error(`删除设备失败: ${error.message}`);
  }
};

  

const clearStaticTable = () => {
  try {
    const data = JSON.parse(staticJsonData.value);
    data.Result = JSON.stringify([]);
    data.Total = 0;
    data.Timestamp = new Date().toISOString().slice(0, 19).replace('T', ' ');

    staticJsonData.value = JSON.stringify(data, null, 2);
    ElMessage.success("已清空表格");
  } catch (error) {
    ElMessage.error(`清空表格失败: ${error.message}`);
  }
};

const getPreviewData = () => {
  // 所有模式都返回 staticJsonData (统一数据格式)
  return staticJsonData.value;
};

// Token认证相关方法
const onAuthTypeChange = () => {
  // 清理之前的定时器
  if (tokenRefreshTimer.value) {
    clearInterval(tokenRefreshTimer.value);
    tokenRefreshTimer.value = null;
  }
  if (customTokenRefreshTimer.value) {
    clearInterval(customTokenRefreshTimer.value);
    customTokenRefreshTimer.value = null;
  }

  // 如果是接口获取token类型，启动自动刷新
  if (formData.config.auth.type === 'bearer' && formData.config.auth.tokenSource === 'api') {
    setupTokenRefresh();
  }
  if (formData.config.auth.type === 'customtoken' && formData.config.auth.customTokenSource === 'api') {
    setupCustomTokenRefresh();
  }
};

const fetchToken = async () => {
  if (!formData.config.auth.tokenUrl) {
    ElMessage.error('请配置认证接口地址');
    return;
  }

  fetchingToken.value = true;
  try {
    const requestConfig = {
      method: formData.config.auth.tokenMethod,
      headers: {
        'Content-Type': 'application/json'
      }
    };

    // 处理请求参数
    let requestBody = null;
    if (formData.config.auth.tokenParams) {
      try {
        requestBody = JSON.parse(formData.config.auth.tokenParams);
      } catch (error) {
        ElMessage.error('请求参数JSON格式错误');
        return;
      }
    }

    if (formData.config.auth.tokenMethod === 'POST' && requestBody) {
      requestConfig.body = JSON.stringify(requestBody);
    }

    // 发送请求获取token
    const response = await fetch(formData.config.auth.tokenUrl, requestConfig);

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }

    const data = await response.json();

    // 根据配置的路径提取token
    const token = extractValueByPath(data, formData.config.auth.tokenPath);

    if (!token) {
      throw new Error(`无法从响应中提取token，路径: ${formData.config.auth.tokenPath}`);
    }

    formData.config.auth.token = token;
    formData.config.auth.lastTokenFetch = new Date().toLocaleString();

    ElMessage.success('Token获取成功');

    // 设置自动刷新
    setupTokenRefresh();

  } catch (error) {
    ElMessage.error('Token获取失败: ' + error.message);
    console.error('Token fetch error:', error);
  } finally {
    fetchingToken.value = false;
  }
};

const extractValueByPath = (obj, path) => {
  return path.split('.').reduce((current, key) => {
    return current && current[key] !== undefined ? current[key] : null;
  }, obj);
};

const setupTokenRefresh = () => {
  // 清理现有定时器
  if (tokenRefreshTimer.value) {
    clearInterval(tokenRefreshTimer.value);
  }

  // 设置新的定时器
  if (formData.config.auth.refreshInterval > 0) {
    const intervalMs = formData.config.auth.refreshInterval * 60 * 1000; // 转换为毫秒
    tokenRefreshTimer.value = setInterval(() => {
      fetchToken();
    }, intervalMs);
  }
};

// 自定义Token相关方法
const fetchCustomToken = async () => {
  if (!formData.config.auth.customTokenUrl) {
    ElMessage.error('请配置自定义Token认证接口地址');
    return;
  }

  fetchingCustomToken.value = true;
  try {
    const requestConfig = {
      method: formData.config.auth.customTokenMethod,
      headers: {
        'Content-Type': 'application/json'
      }
    };

    // 处理请求参数
    let requestBody = null;
    if (formData.config.auth.customTokenParams) {
      try {
        requestBody = JSON.parse(formData.config.auth.customTokenParams);
      } catch (error) {
        ElMessage.error('请求参数JSON格式错误');
        return;
      }
    }

    if (formData.config.auth.customTokenMethod === 'POST' && requestBody) {
      requestConfig.body = JSON.stringify(requestBody);
    }

    // 发送请求获取token
    const response = await fetch(formData.config.auth.customTokenUrl, requestConfig);

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }

    const data = await response.json();

    // 根据配置的路径提取token
    const token = extractValueByPath(data, formData.config.auth.customTokenPath);

    if (!token) {
      throw new Error(`无法从响应中提取token，路径: ${formData.config.auth.customTokenPath}`);
    }

    formData.config.auth.customToken = token;
    formData.config.auth.lastCustomTokenFetch = new Date().toLocaleString();

    ElMessage.success('自定义Token获取成功');

    // 设置自动刷新
    setupCustomTokenRefresh();

  } catch (error) {
    ElMessage.error('自定义Token获取失败: ' + error.message);
    console.error('Custom token fetch error:', error);
  } finally {
    fetchingCustomToken.value = false;
  }
};

const setupCustomTokenRefresh = () => {
  // 清理现有定时器
  if (customTokenRefreshTimer.value) {
    clearInterval(customTokenRefreshTimer.value);
  }

  // 设置新的定时器
  if (formData.config.auth.customRefreshInterval > 0) {
    const intervalMs = formData.config.auth.customRefreshInterval * 60 * 1000; // 转换为毫秒
    customTokenRefreshTimer.value = setInterval(() => {
      fetchCustomToken();
    }, intervalMs);
  }
};

const getCustomTokenPreview = () => {
  const authConfig = formData.config.auth;
  if (!authConfig.customToken || !authConfig.customTokenFormat) {
    return '';
  }
  return authConfig.customTokenFormat.replace('{token}', authConfig.customToken);
};

const buildAuthHeaders = () => {
  const authHeaders = {};
  const authConfig = formData.config.auth;

  switch (authConfig.type) {
    case 'bearer':
      if (authConfig.token) {
        authHeaders['Authorization'] = `Bearer ${authConfig.token}`;
      }
      break;

    case 'customtoken':
      if (authConfig.customToken) {
        const headerName = authConfig.customTokenHeader || 'Authorization';
        const tokenFormat = authConfig.customTokenFormat || 'Bearer {token}';
        const formattedToken = tokenFormat.replace('{token}', authConfig.customToken);
        authHeaders[headerName] = formattedToken;
      }
      break;

    case 'apikey':
      if (authConfig.keyValue) {
        if (authConfig.keyLocation === 'header') {
          authHeaders[authConfig.keyName] = authConfig.keyValue;
        }
        // Query参数处理在发送请求时处理
      }
      break;

    case 'basic':
      if (authConfig.username && authConfig.password) {
        const credentials = btoa(`${authConfig.username}:${authConfig.password}`);
        authHeaders['Authorization'] = `Basic ${credentials}`;
      }
      break;

    case 'custom':
      if (authConfig.headerName && authConfig.headerValue) {
        authHeaders[authConfig.headerName] = authConfig.headerValue;
      }
      break;
  }

  return authHeaders;
};

// 监听props变化，初始化表单数据(按类型切到对应tab回填)
watch(
  () => props.dataset,
  newDataset => {
    if (!newDataset) return;
    if (newDataset.type === "iot") {
      Object.assign(iotConfig, {
        name: newDataset.name || "",
        description: newDataset.description || "",
        deviceId: newDataset.deviceId ?? null,
        deviceName: newDataset.deviceName || "",
        deviceTypeCode: newDataset.deviceTypeCode || "",
        mode: newDataset.mode || "realtime",
        points: [...(newDataset.points || [])],
        historyHours: newDataset.historyHours || 24,
        historyMode: newDataset.historyMode || "auto"
      });
      activeTab.value = "iot";
      return;
    }
    Object.assign(formData, newDataset);
    if (["api", "mqtt", "static"].includes(newDataset.type)) {
      activeTab.value = newDataset.type;
    }
  },
  { immediate: true }
);

// 监听token源类型变化
watch(
  () => formData.config.auth.tokenSource,
  (newSource) => {
    if (newSource === 'api') {
      setupTokenRefresh();
    } else {
      // 清理自动刷新定时器
      if (tokenRefreshTimer.value) {
        clearInterval(tokenRefreshTimer.value);
        tokenRefreshTimer.value = null;
      }
    }
  }
);

// 监听自定义token源类型变化
watch(
  () => formData.config.auth.customTokenSource,
  (newSource) => {
    if (newSource === 'api') {
      setupCustomTokenRefresh();
    } else {
      // 清理自动刷新定时器
      if (customTokenRefreshTimer.value) {
        clearInterval(customTokenRefreshTimer.value);
        customTokenRefreshTimer.value = null;
      }
    }
  }
);

// 组件销毁时清理定时器
onBeforeUnmount(() => {
  if (tokenRefreshTimer.value) {
    clearInterval(tokenRefreshTimer.value);
  }
  if (customTokenRefreshTimer.value) {
    clearInterval(customTokenRefreshTimer.value);
  }
});

// 暴露方法给父组件
defineExpose({
  // 获取静态设备数据
  getStaticDeviceData: () => {
    try {
      const data = JSON.parse(staticJsonData.value);
      if (data.Result && typeof data.Result === 'string') {
        return {
          data: JSON.parse(data.Result),
          type: 'static'
        };
      }
    } catch (error) {
      console.error('获取静态设备数据失败:', error);
    }
    return { data: [], type: 'static' };
  },

  // 获取所有数据集配置(面板内各tab当前已填配置的快照)
  getDatasets: () => {
    const list: any[] = [];
    if (formData.config.url || formData.config.deviceUrl) {
      list.push({ type: "api", ...JSON.parse(JSON.stringify(formData)) });
    }
    list.push({ type: "static", data: staticJsonData.value });
    if (iotConfig.deviceId) {
      list.push({ type: "iot", ...JSON.parse(JSON.stringify(iotConfig)) });
    }
    return list;
  }
});
</script>

<style scoped lang="scss">
.dataset-dialog-content {
  height: 70vh;
  overflow: hidden;

  :deep(.el-tabs--border-card) {
    height: 100%;
    border: none;
    box-shadow: none;

    .el-tabs__header {
      background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
      border-bottom: 2px solid #dee2e6;
      margin: 0;

      .el-tabs__nav-wrap {
        &::after {
          display: none;
        }
      }

      .el-tabs__item {
        border-left: 1px solid #dee2e6;
        border-top: 1px solid #dee2e6;
        border-right: none;
        border-bottom: none;
        background: #f8f9fa;
        color: #6c757d;
        font-weight: 500;
        padding: 0 24px;
        height: 48px;
        line-height: 48px;
        font-size: 14px;

        &:first-child {
          border-left: none;
        }

        &.is-active {
          background: #ffffff;
          color: #495057;
          border-bottom: 2px solid #667eea;
          font-weight: 600;
        }

        &:hover:not(.is-active) {
          background: #e9ecef;
          color: #495057;
        }
      }
    }

    .el-tabs__content {
      height: calc(100% - 48px);
      overflow-y: auto;
      padding: 20px;
      background: #ffffff;

      .el-tab-pane {
        height: 100%;
      }
    }
  }
}

.dataset-config-section {
  height: 100%;
  overflow-y: auto;
}

.config-section {
  margin-bottom: 24px;
  padding: 16px;
  background: #ffffff;
  border: 1px solid #e9ecef;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
  transition: all 0.3s ease;

  &:hover {
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    border-color: #ced4da;
  }

  &:last-child {
    margin-bottom: 0;
  }

  h4 {
    font-size: 14px;
    font-weight: 600;
    color: #495057;
    margin-bottom: 16px;
    padding: 8px 12px;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    border-radius: 6px;
    border-left: none;
    display: flex;
    align-items: center;

    &::before {
      content: "";
      width: 4px;
      height: 16px;
      background: rgba(255, 255, 255, 0.8);
      border-radius: 2px;
      margin-right: 8px;
    }
  }

  :deep(.el-form-item) {
    margin-bottom: 16px;

    .el-form-item__label {
      font-size: 13px;
      color: #6c757d;
      font-weight: 500;
      line-height: 1.4;
    }

    .el-input,
    .el-select {
      .el-input__wrapper {
        border-radius: 6px;
        box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
        transition: all 0.3s ease;

        &:hover {
          box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
        }

        &.is-focus {
          box-shadow: 0 0 0 2px rgba(102, 126, 234, 0.25);
        }
      }
    }
  }
}

.headers-config {
  .header-item {
    display: flex;
    align-items: center;
    margin-bottom: 8px;
  }

  .auth-headers-info {
    margin-top: 12px;

    :deep(.el-alert) {
      background-color: rgba(102, 126, 234, 0.1);
      border: 1px solid rgba(102, 126, 234, 0.2);
      border-radius: 6px;

      .el-alert__content {
        font-size: 12px;
        color: #667eea;
      }

      .el-alert__icon {
        color: #667eea;
      }
    }
  }
}

// Token认证相关样式
.token-status {
  margin-left: 12px;
  font-size: 12px;
  color: #28a745;
  display: inline-flex;
  align-items: center;

  &::before {
    content: "✓";
    margin-right: 4px;
    font-weight: bold;
  }
}

.token-format-help {
  margin-top: 4px;

  small {
    color: #6c757d;
    font-size: 11px;
    line-height: 1.3;
    display: block;
  }
}

.table-info {
  margin-bottom: 16px;

  :deep(.el-alert) {
    background-color: rgba(54, 132, 254, 0.08);
    border: 1px solid rgba(54, 132, 254, 0.2);
    border-radius: 6px;

    .el-alert__content {
      font-size: 13px;
      line-height: 1.5;

      p {
        margin: 4px 0;
        color: #606266;

        &:first-child {
          margin-top: 0;
        }

        &:last-child {
          margin-bottom: 0;
        }
      }
    }

    .el-alert__icon {
      color: #3684fe;
    }
  }
}

:deep(.el-radio-group) {
  .el-radio-button {
    .el-radio-button__inner {
      border-radius: 6px;
      margin-right: 8px;
      border: 1px solid #d0d7de;
      background-color: #f6f8fa;
      color: #656d76;

      &:hover {
        background-color: #e9ecef;
        border-color: #ced4da;
      }
    }

    &.is-active {
      .el-radio-button__inner {
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        border-color: #667eea;
        color: white;
        box-shadow: 0 2px 4px rgba(102, 126, 234, 0.3);
      }
    }
  }
}

.test-area {
  .test-result {
    margin-top: 16px;

    .result-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 8px;
      padding: 8px 12px;
      background: #f8f9fa;
      border-radius: 4px;

      .test-time {
        font-size: 12px;
        color: #6c757d;
      }
    }
  }
}

.static-data-editor {
  .data-format-selector {
    margin-bottom: 16px;
  }

  .json-editor {
    margin-bottom: 16px;
  }

  .table-editor {
    margin-bottom: 16px;

    .table-controls {
      margin-bottom: 12px;
      display: flex;
      gap: 8px;
    }
  }

  .data-preview {
    margin-top: 16px;
    padding: 16px;
    background: #f8f9fa;
    border-radius: 6px;

    h5 {
      margin: 0 0 12px 0;
      font-size: 14px;
      font-weight: 600;
      color: #495057;
    }
  }
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding: 16px 20px;

  .el-button {
    min-width: 80px;
    height: 36px;
    border-radius: 6px;
    font-weight: 500;

    &:first-child {
      background: #6c757d;
      border-color: #6c757d;
      color: white;

      &:hover {
        background: #5a6268;
        border-color: #545b62;
      }
    }

    &.el-button--primary {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      border: none;

      &:hover {
        background: linear-gradient(135deg, #5a67d8 0%, #6b46c1 100%);
        transform: translateY(-1px);
        box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
      }
    }

    &.el-button--success {
      background: linear-gradient(135deg, #28a745 0%, #20c997 100%);
      border: none;

      &:hover {
        background: linear-gradient(135deg, #218838 0%, #1e7e34 100%);
        transform: translateY(-1px);
        box-shadow: 0 4px 12px rgba(40, 167, 69, 0.3);
      }
    }
  }
}

.json-help-text {
  margin-bottom: 8px;
  color: #909399;
}

.json-help-text small {
  font-size: 12px;
}
</style>
