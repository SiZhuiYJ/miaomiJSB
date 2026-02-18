import { ref } from "vue";
import { ScheduleApi } from "@/features/schedule/api";
import type { Schedule } from "@/features/schedule/types";
import { ElLoading } from "element-plus";
import { meowMsgError, meowMsgSuccess } from "@/utils/message";

export default function useSchedule() {
    // 所有课表列表
    const schedule = ref<Schedule[]>([]);

    async function initializeData() {
        const loading = ElLoading.service({
            lock: true,
            text: "获取课表数据中...",
            background: "rgba(0, 0, 0, 0.7)"
        });
        try {
            const { data } = await ScheduleApi.Schedule.PostList();
            console.log("获取课表数据", data);
            // schedule.value = data.schedule;
            meowMsgSuccess("课表获取成功");
        } catch (error) {
            schedule.value = [];
            console.log(error);
            meowMsgError("课表获取失败");
        }
        console.log("课表集合", schedule.value)
        loading.close();
    }
    function getScheduleByID(id: number) {
        return schedule.value.find(item => item.id === id);
    }
    return {
        schedule,
        initializeData,
        getScheduleByID
    };
}
