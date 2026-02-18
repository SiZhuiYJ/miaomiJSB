// 定位信息
export type LocationData = {
  /*
   **状态
   */
  status: number;
  /*
   **代码
   */
  code: number;
  /*
   **信息
   */
  info: string;
  /*
   **定位
   */
  position: {
    KL: number;
    className: string;
    kT: number;
    lat: number;
    lng: number;
  };
  /*
   **定位类别
   */
  location_type: string;
  /*
   **消息
   */
  message: string;
  /*
   **精度
   */
  accuracy: number;
  /*
   **是否经过偏移
   */
  isConverted: boolean;
  /*
   **高度
   */
  altitude: string | null;
  /*
   **高度准确性
   */
  altitudeAccuracy: string | null;
  /*
   **标题：
   */
  heading: string | null;
  /*
   **速度
   */
  speed: string | null;
};
// 天气预报
export type WeatherData = {
  /*
   **省/直辖市
   */
  province: string;
  /*
   **城市
   */
  city: string;
  /*
   **城市编号
   */
  adcode: string;
  /*
   **天气
   */
  weather: string;
  /*
   **温度
   */
  temperature: number;
  /*
   **方向
   */
  windDirection: string;
  /*
   **风力
   */
  windPower: string;
  /*
   **湿度
   */
  humidity: string;
  /*
   **更新时间
   */
  reportTime: string;
  /*
   **通知
   */
  info: string;
};
// 未来的天气
export type FutureWeatherData = {
  /*
   **日期
   */
  date: string;
  /*
   **周几
   */
  week: string;
  /*
   **当日天气
   */
  dayWeather: string;
  /*
   **当晚天气
   */
  nightWeather: string;
  /*
   **最高温度
   */
  dayTemp: number;
  /*
   **最低温度
   */
  nightTemp: number;
  /*
   **当日方向
   */
  dayWindDir: string;
  /*
   **当晚方向
   */
  nightWindDir: string;
  /*
   **当日风力
   */
  dayWindPower: string;
  /*
   **当晚风力
   */
  nightWindPower: string;
};
export type AddressComponent = {
  citycode: number;
  adcode: number;
  businessAreas: Array<string | null | undefined>;
  neighborhoodType: string;
  neighborhood: string;
  building: string;
  buildingType: string;
  street: string;
  streetNumber: string;
  province: string;
  city: string;
  district: string;
  towncode: number;
  township: string;
};
export type Regeocode = {
  addressComponent: AddressComponent;
  formattedAddress: string;
  roads: Array<string | null | undefined>;
  crosses: Array<string | null | undefined>;
  pois: Array<string | null | undefined>;
};
export type Address = {
  info: string;
  regeocode: Regeocode;
};
