import type gsap from "gsap";

export type NotificationDirection =
  | "ltr"
  | "rtl"
  | "ttb"
  | "btt"
  | "center"
  | "vSplit"
  | "ripple"
  | "spotlight"
  | "fade";

export interface NotificationMessage {
  id: number;
  content: string;
  color: string;
  duration: number;
  closable: boolean;
  direction: NotificationDirection;
  count: number;
  progress: number;
  isRemoving: boolean;
  tween: gsap.core.Tween | null;
}

export interface NotificationOptions {
  content: string;
  color?: string;
  duration?: number;
  closable?: boolean;
  direction?: NotificationDirection;
}

export interface NotificationForm {
  content: string;
  color: string;
  duration: number;
  closable: boolean;
  direction: NotificationDirection;
}

export interface NotificationDirectionOption {
  n: string;
  v: NotificationDirection;
}
