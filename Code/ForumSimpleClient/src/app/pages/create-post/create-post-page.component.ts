import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ForumService } from '../../services/forum.service';

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './create-post-page.component.html',
  styleUrl: './create-post-page.component.scss'
})
export class CreatePostPageComponent implements OnInit {
  forumId = 0;
  title = '';
  content = '';
  errorMessage = '';
  isSubmitting = false;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly forumService: ForumService
  ) {}

  ngOnInit(): void {
    const rawForumId = this.route.snapshot.paramMap.get('forumId');
    this.forumId = Number(rawForumId);
    if (!Number.isFinite(this.forumId) || this.forumId <= 0) {
      this.errorMessage = 'Invalid forum id.';
    }
  }

  submit(): void {
    if (this.forumId <= 0) {
      return;
    }

    this.errorMessage = '';
    this.isSubmitting = true;
    this.forumService
      .createPost({
        forumId: this.forumId,
        title: this.title.trim(),
        content: this.content.trim()
      })
      .subscribe({
        next: () => {
          this.isSubmitting = false;
          this.router.navigate(['/forum', this.forumId]);
        },
        error: (error: unknown) => {
          this.isSubmitting = false;
          this.errorMessage = error instanceof Error ? error.message : 'Failed to create post.';
        }
      });
  }
}
