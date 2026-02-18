// 基础响应类型
export interface BaseResponse<T = any> {
    code: number;
    message: string;
    data: T;
    success: boolean;
}

// 分页参数
export interface PaginationParams {
    page: number;
    pageSize: number;
}

// 分页响应
export interface PaginationResponse<T = any> {
    list: T[];
    total: number;
    page: number;
    pageSize: number;
}

// 课程时间段类型
export interface CourseTimeSlot {
    /** 课程地点 */
    location: string;
    /** 教师姓名 */
    teacher: string;
    /** 周次数组，如 [1,2,3,4,5] */
    weeks: number[];
    /** 节次数组，如 [1,2] 表示第1-2节 */
    sections: number[];
    /** 周几，1-7 表示周一至周日 */
    weekday: number;
}

// 课程项类型
export interface CourseItem {
    /** 课程表ID */
    id: number;
    /** 关联的课表ID */
    schedule_id: number;
    /** 课程名称 */
    course_name: string;
    /** 课程颜色，十六进制颜色码，如 #FF0000 */
    color: string;
    /** 课程时间段数组 */
    time_slots: CourseTimeSlot[];
    /** 备注 */
    remark?: string;
    /** 创建时间 */
    create_time: string;
    /** 更新时间 */
    update_time: string;
    /** 删除标记 */
    is_deleted: 0 | 1;
    /** 扩展字段 */
    ext_attr1?: string;
    ext_attr2?: string;
    ext_attr3?: string;
}

// 课表类型
export interface Schedule {
    /** 课表ID */
    id: number;
    /** 用户ID */
    userId: number;
    /** 课表名称 */
    scheduleName: string;
    /** 开课时间，格式：YYYY-MM-DD */
    startTime: string;
    /** 本学期周数 */
    weekCount: number;
    /** 作息时间表，如 ["08:00-08:45", "08:55-09:40"] */
    timetable: string[];
    /** 备注 */
    remark?: string;
    /** 创建时间 */
    createTime: string;
    /** 更新时间 */
    updateTime: string;
    /** 删除标记 */
    is_deleted: 0 | 1;
    /** 扩展字段 */
    ext_attr1?: string;
    ext_attr2?: string;
    ext_attr3?: string;
}

// 创建课表请求参数
export interface CreateScheduleRequest {
    user_id: number;
    start_time: string;
    week_count: number;
    timetable: string[];
    ext_attr1?: string;
    ext_attr2?: string;
    ext_attr3?: string;
}

// 更新课表请求参数
export interface UpdateScheduleRequest extends Partial<CreateScheduleRequest> {
    id: number;
}

// 创建课程请求参数
export interface CreateCourseRequest {
    schedule_id: number;
    color: string;
    time_slots: CourseTimeSlot[];
    ext_attr1?: string;
    ext_attr2?: string;
    ext_attr3?: string;
}

// 更新课程请求参数
export interface UpdateCourseRequest extends Partial<CreateCourseRequest> {
    id: number;
}

// 查询课表参数
export interface QueryScheduleParams extends PaginationParams {
    user_id?: number;
    start_time?: string;
    keyword?: string;
}

// 查询课程参数
export interface QueryCourseParams extends PaginationParams {
    schedule_id?: number;
    teacher?: string;
    location?: string;
}

// 课程表显示项（用于前端渲染）
export interface DisplayCourse {
    id: number;
    schedule_id: number;
    color: string;
    /** 课程名称（可以从其他字段推导或单独存储） */
    name: string;
    /** 教师姓名（从第一个时间段获取） */
    teacher: string;
    /** 所有时间段 */
    time_slots: CourseTimeSlot[];
    /** 用于渲染的显示文本 */
    displayText: string;
}

// 一周课程数据
export interface WeekCourseData {
    [weekday: number]: DisplayCourse[];
}

// 课表查询结果
export interface ScheduleWithCourses extends Schedule {
    courses: CourseItem[];
    week_course_data: WeekCourseData;
}

// 批量操作相关类型
export interface BatchCreateCoursesRequest {
    schedule_id: number;
    courses: CreateCourseRequest[];
}

export interface BatchUpdateCoursesRequest {
    courses: UpdateCourseRequest[];
}

export interface BatchDeleteRequest {
    ids: number[];
}

// 响应课表列表
export interface ScheduleListResponse {
    id: number; // '课表ID',
    start_time: string; // '开课时间',
    week_count: number; //  '本学期周数',
    timetable: string; //  '作息表',
    create_time: string; // '创建时间',
    update_time: string; // '更新时间',
}