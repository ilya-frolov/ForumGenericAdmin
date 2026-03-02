import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ForumPostPreview } from '../../models/forum.models';
import { ForumService } from '../../services/forum.service';
import { AuthService } from '../../services/auth.service';

@Component({
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './forum-page.component.html',
  styleUrl: './forum-page.component.scss'
})
export class ForumPageComponent implements OnInit {
  forumId = 0;
  posts: ForumPostPreview[] = [];
  errorMessage = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly forumService: ForumService,
    public readonly authService: AuthService
  ) {}

  ngOnInit(): void {
    const rawId = this.route.snapshot.paramMap.get('id');
    this.forumId = Number(rawId);
    if (!Number.isFinite(this.forumId) || this.forumId <= 0) {
      this.errorMessage = 'Invalid forum id.';
      return;
    }

    this.loadPosts();
  }

  private loadPosts(): void {
    this.forumService.getForumPosts(this.forumId).subscribe({
      next: (posts) => {
        this.posts = posts;
      },
      error: (error: unknown) => {
        this.errorMessage = error instanceof Error ? error.message : 'Failed to load posts.';
      }
    });
  }

  trackByPostId(index: number, post: ForumPostPreview): number {
    return post.postId;
  }
}
