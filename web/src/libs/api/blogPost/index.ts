// @/libs/api/blogPost/index.ts
import http from "@/libs/http";
import type { ResponseData } from "@/libs/http/type";
import type {
  blogPost,
  operation,
  Tag,
  AddTag,
  UploadBlog
} from "@/libs/api/blogPost/type";

interface BlogList {
  blogs: blogPost[];
}
interface Blog {
  blog: blogPost;
}
interface BlogTags {
  tags: Tag[];
}
export const BlogPostApi = {
  // 获取文章列表
  PostBlogGetList() {
    return http.get<ResponseData<BlogList>>(`/Blog/GetAllBlogs`);
  },
  // 获取文章详情
  PostBlogPostDetail(id: number) {
    return http.get<ResponseData<Blog>>(`/Blog/GetBlogById/${id}`);
  },
  // 获取文章标签列表
  PostBlogGetTags() {
    return http.get<ResponseData<BlogTags>>(`/Blog/GetBlogTags`);
  },
  // 操作文章
  PostBlogPostEdit(blog: UploadBlog, operation: operation) {
    return http.post<ResponseData<Blog>>(`Blog/UploadBlog/${operation}`, blog);
  },
  // 添加标签
  PostBlogAddTag(blogTag: AddTag) {
    return http.post<ResponseData<{ tagId: number }>>(
      `/Blog/AddBlogTag`,
      blogTag
    );
  },
};
