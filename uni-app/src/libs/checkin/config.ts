import createHttp from '@/libs/http';
import { API_BASE_URL } from '@/config';
export const http = createHttp(API_BASE_URL);
export default http;
