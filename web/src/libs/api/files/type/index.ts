export interface MediaTable {
  id: number;
  url: string;
  tags: number[];
  size: number;
  createTime: string;
  uploadTime: string;
  createAddress: {
    longitude: number;
    latitude: number;
    address: string;
  };
  deviceName: string;
  name: string;
  type: string;
}
export interface options {
  label: string;
  value: string;
}
export interface searchType {
  name: string;
  type: string;
  tags: number[];
  createAddress: string;
  deviceName: string;
  dateCreate: string[];
  dateUpload: string[];
}
export interface Media {
  mediaId: number;
  url: string;
  tags: string;
  size: number;
  createTime: string;
  uploadTime: string;
  Longitude: number;
  Latitude: number;
  createAddress: string;
  deviceName: string;
  name: string;
  type: string;
}
export interface tag {
  tagName: string;
  tagId: string;
}
