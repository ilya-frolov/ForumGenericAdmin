import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ForumPostDetails } from '../../models/forum.models';
import { ForumService } from '../../services/forum.service';
import { AuthService } from '../../services/auth.service';

@Component({
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './post-page.component.html',
  styleUrl: './post-page.component.scss'
})
export class PostPageComponent implements OnInit {
  postId = 0;
  post: ForumPostDetails | null = null;
  commentText = '';
  errorMessage = '';
  isSubmitting = false;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly forumService: ForumService,
    public readonly authService: AuthService
  ) {}

  ngOnInit(): void {
    const rawId = this.route.snapshot.paramMap.get('id');
    this.postId = Number(rawId);
    if (!Number.isFinite(this.postId) || this.postId <= 0) {
      this.errorMessage = 'Invalid post id.';
      return;
    }

    this.loadPost();
  }

  addComment(): void {
    if (!this.post || !this.commentText.trim()) {
      return;
    }

    this.isSubmitting = true;
    this.forumService
      .addComment({
        postId: this.post.postId,
        content: this.commentText.trim()
      })
      .subscribe({
        next: () => {
          this.commentText = '';
          this.isSubmitting = false;
          this.loadPost();
        },
        error: (error: unknown) => {
          this.isSubmitting = false;
          this.errorMessage = error instanceof Error ? error.message : 'Failed to add comment.';
        }
      });
  }

  deleteComment(commentId: number): void {
    this.forumService.deleteComment({ commentId }).subscribe({
      next: () => {
        this.loadPost();
      },
      error: (error: unknown) => {
        this.errorMessage = error instanceof Error ? error.message : 'Failed to delete comment.';
      }
    });
  }

  private loadPost(): void {
    this.forumService.getPost(this.postId).subscribe({
      next: (post) => {
        this.post = post;
      },
      error: (error: unknown) => {
        this.errorMessage = error instanceof Error ? error.message : 'Failed to load post.';
      }
    });
  }

  trackByCommentId(index: number, comment: { commentId: number }): number {
    return comment.commentId;
  }
}
