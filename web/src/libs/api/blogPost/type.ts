export interface blogPost {
  id: number; // 博客id
  title: string; // 标题
  content: string; // 内容
  authorId: number; // 作者id
  author: string; // 作者
  createTime: string; // 发布时间
  updateTime: string; // 更新时间
  summary: string; // 封面内容
  tags: string; // 标签
}
export type operation = "add" | "update";

export interface Tag {
  tagName: string;
  id: number;
  tagColor: string;
}
// 上传博客
export interface UploadBlog {
  id: number,
  title: string,
  summary: string,
  content: string,
  tags: string
}
// 添加标签
export interface AddTag {
  TagColor: string;
  TagDescription: string;
  TagIcon: string;
  TagName: string;
}

export interface EditBlog {
  id: number,
  title: string,
  summary: string,
  content: string,
  tags: string[]
}
