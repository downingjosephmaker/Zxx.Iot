// 网络设备图片资源索引文件
// 将 base64 图片转换为可引入的资源

// CPE 2 设备图片
import cpe2Image from './cpe2.png';

// 路由器图片
import routerImage from './router.png';

// 工业安全隔离与信息交换系统GAP图片
import gapSystemImage from './gap-system.png';

// 摄像头相关图片
import cameraLeftImage from './camera-left.png';
import ballCameraImage from './ball-camera.png';

// 服务器图片
import server1UImage from './server-1u.png';
import server2UImage from './server-2u.png';

// 电话网关图片
import phoneGatewayImage from './phone-gateway.png';

// 导出所有图片资源
export const NetworkDeviceImages = {
  'CPE 2': cpe2Image,
  '路由器': routerImage,
  '工业安全隔离与信息交换系统GAP': gapSystemImage,
  'Camera_枪机_向左': cameraLeftImage,
  '球机': ballCameraImage,
  '2U_Server': server2UImage,
  '1U_Server': server1UImage,
  '电话网关': phoneGatewayImage,
} as const;

// 获取设备图片的辅助函数
export function getDeviceImage(deviceType: string): string {
  return NetworkDeviceImages[deviceType as keyof typeof NetworkDeviceImages] || '';
}

// 获取所有支持的设备类型
export function getSupportedDeviceTypes(): string[] {
  return Object.keys(NetworkDeviceImages);
}

// 检查设备类型是否支持
export function isDeviceTypeSupported(deviceType: string): boolean {
  return deviceType in NetworkDeviceImages;
}
