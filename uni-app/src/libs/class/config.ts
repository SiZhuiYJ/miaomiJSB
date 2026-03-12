import createHttp from '@/libs/http';
const CLASS_API_BASE_URL = 'https://www.meowmemoirs.cn/MeowMemoirs';
export const http = createHttp(CLASS_API_BASE_URL);
export default http;