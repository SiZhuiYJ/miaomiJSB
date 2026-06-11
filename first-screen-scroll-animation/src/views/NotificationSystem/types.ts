import type gsap from "gsap";

export interface NotificationMessage {
  id: number;
  content: string;
  color: string;
  duration?: number;
  closable?: boolean;
  direction?:
    | "ltr"
    | "rtl"
    | "ttb"
    | "btt"
    | "center"
    | "vSplit"
    | "ripple"
    | "spotlight"
    | "fade";
  count: number;
  progress: number;
  isRemoving: boolean;
  tween?: gsap.core.Tween | null;
}
