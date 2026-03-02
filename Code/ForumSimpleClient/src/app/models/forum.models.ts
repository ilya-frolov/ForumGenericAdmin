export interface ForumLookup {
  forumId: number;
  forumName: string;
  managersOnlyPosting: boolean;
}

export interface ForumsData {
  forums: ForumLookup[];
  isSiteLocked: boolean;
}

export interface ForumPostPreview {
  postId: number;
  forumId: number;
  userId: number;
  userName: string;
  title: string;
  contentPreview: string;
  createDate: string;
  commentsCount: number;
}

export interface PostsData {
  posts: ForumPostPreview[];
}

export interface ForumComment {
  commentId: number;
  userId: number;
  userName: string;
  content: string;
  createDate: string;
}

export interface ForumPostDetails {
  postId: number;
  forumId: number;
  forumName: string;
  userId: number;
  userName: string;
  title: string;
  content: string;
  createDate: string;
  comments: ForumComment[];
}

export interface PostData {
  post: ForumPostDetails | null;
}

export interface AuthResult {
  userId: number;
  userName: string;
  isManager: boolean;
  profilePicturePath?: string | null;
  token: string;
  tokenExpirationDate: string;
}

export interface AuthData {
  auth: AuthResult;
}

export interface CreatePostRequest {
  forumId: number;
  title: string;
  content: string;
}

export interface CreateCommentRequest {
  postId: number;
  content: string;
}

export interface DeleteCommentRequest {
  commentId: number;
}
