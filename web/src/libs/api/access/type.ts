export interface IPLocation {
  ip: string,
  country: string,
  area: string,
  isp: string,
  source: string,
}
export interface IPInfo {
  ip: string,
  as: {
    number: number,
    name: string,
    info: string
  },
  addr: string,
  country: {
    code: string,
    name: string
  },
  registeredCountry: {
    code: string,
    name: string
  },
  regions: string[],
  regionsShort: string[],
  type: string
}