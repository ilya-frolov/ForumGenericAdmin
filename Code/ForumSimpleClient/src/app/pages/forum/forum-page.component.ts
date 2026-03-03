import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ForumLookup, ForumPostPreview, ForumsData } from '../../models/forum.models';
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
  forum: ForumLookup | null = null;
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
    this.loadForumInfo();
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

  private loadForumInfo(): void {
    this.forumService.getForums().subscribe({
      next: (data: ForumsData) => {
        this.forum = data.forums.find((forum) => forum.forumId === this.forumId) ?? null;
      },
      error: () => {
        // Keep null forum metadata; posting button will stay conservative.
        this.forum = null;
      }
    });
  }

  canCreatePost(): boolean {
    let canCreatePost = false;
    const currentUser = this.authService.currentUser();

    if (currentUser && this.forum) {
      canCreatePost = !this.forum.managersOnlyPosting || currentUser.isManager;
    }

    return canCreatePost;
  }

  trackByPostId(index: number, post: ForumPostPreview): number {
    return post.postId;
  }
}
