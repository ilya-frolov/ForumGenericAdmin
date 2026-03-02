import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { BaseService } from './base.service';
import {
  CreateCommentRequest,
  CreatePostRequest,
  DeleteCommentRequest,
  ForumPostDetails,
  ForumPostPreview,
  ForumsData,
  ForumLookup,
  PostData,
  PostsData
} from '../models/forum.models';

@Injectable({ providedIn: 'root' })
export class ForumService extends BaseService {
  getForums(): Observable<ForumsData> {
    return this.get<ForumsData>('forum/forums').pipe(
      map((response) => {
        if (!response.result) {
          throw new Error(response.error ?? 'Failed to load forums.');
        }

        return response.data;
      })
    );
  }

  getForumPosts(forumId: number, take: number = 20): Observable<ForumPostPreview[]> {
    return this.get<PostsData>(`forum/posts/${forumId}?take=${take}`).pipe(
      map((response) => {
        if (!response.result) {
          throw new Error(response.error ?? 'Failed to load posts.');
        }

        return response.data.posts;
      })
    );
  }

  getPost(postId: number): Observable<ForumPostDetails | null> {
    return this.get<PostData>(`forum/post/${postId}`).pipe(
      map((response) => {
        if (!response.result) {
          throw new Error(response.error ?? 'Failed to load post.');
        }

        return response.data.post;
      })
    );
  }

  createPost(request: CreatePostRequest): Observable<number> {
    return this.post<{ postId: number }>('forum/post', request).pipe(
      map((response) => {
        if (!response.result || !response.data) {
          throw new Error(response.error ?? 'Failed to create post.');
        }

        return response.data.postId;
      })
    );
  }

  addComment(request: CreateCommentRequest): Observable<number> {
    return this.post<{ commentId: number }>('forum/comment', request).pipe(
      map((response) => {
        if (!response.result || !response.data) {
          throw new Error(response.error ?? 'Failed to add comment.');
        }

        return response.data.commentId;
      })
    );
  }

  deleteComment(request: DeleteCommentRequest): Observable<boolean> {
    return this.deleteWithBody<boolean>('forum/comment', request).pipe(
      map((response) => {
        if (!response.result) {
          throw new Error(response.error ?? 'Failed to delete comment.');
        }

        return true;
      })
    );
  }
}
