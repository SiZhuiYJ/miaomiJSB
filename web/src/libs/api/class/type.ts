// 课程类
export interface Class {
    id: number; // 课程id
    name: string; // 课程名
    location: string; // 地点
    dayOfWeek: 1 | 2 | 3 | 4 | 5 | 6 | 7; // 周几
    week: number[]; // 周数
    number: number[]; // 节次
    teacher: string; // 教师
    color: string; // 颜色
    remark?: string; // 备注
}

export interface ClassDto {
    id: number; // 课程id
    userId: number; // 课程用户
    className: string; // 课程名称
    location: string; // 课程地址
    dayOfWeek: 1 | 2 | 3 | 4 | 5 | 6 | 7;
    weekList: string; // 周数
    sessionList: string; // 课程节次
    teacher: string; // 课表老师
    color: string; // 课表颜色
    remark?: string; // 备注
    isDeleted: 0 | 1; // 删除状态
    createTime: string; // 创建时间
    updateTime: string; // 更新时间
}

export interface Event {
    id: number;
    date: string;
    type:
    | "anniversary"
    | "countdown"
    | "festival"
    | "important"
    | "birthday"
    | "other";
    info: string;
    color: string;
}