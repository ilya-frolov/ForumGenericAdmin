import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ForumLookup, ForumsData } from '../../models/forum.models';
import { ForumService } from '../../services/forum.service';

@Component({
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './forums-page.component.html',
  styleUrl: './forums-page.component.scss'
})
export class ForumsPageComponent implements OnInit {
  forums: ForumLookup[] = [];
  isSiteLocked = false;
  errorMessage = '';

  constructor(private readonly forumService: ForumService) {}

  ngOnInit(): void {
    this.forumService.getForums().subscribe({
      next: (data: ForumsData) => {
        this.forums = data.forums;
        this.isSiteLocked = data.isSiteLocked;
      },
      error: (error: unknown) => {
        this.errorMessage = error instanceof Error ? error.message : 'Failed to load forums.';
      }
    });
  }

  trackByForumId(index: number, forum: ForumLookup): number {
    return forum.forumId;
  }
}
