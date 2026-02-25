import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CardModule } from 'primeng/card';
import { LoginService } from '../../services/login.service';
import { ConnectedUser } from '../../interfaces/models';

@Component({
  selector: 'app-admin-home',
  templateUrl: './admin-home.component.html',
  standalone: true,
  imports: [
    CommonModule,
    CardModule
  ]
})
export class AdminHomeComponent implements OnInit {
  userDisplayName: string = 'User';

  constructor(private loginService: LoginService) { }

  ngOnInit(): void {
    const currentUser: ConnectedUser | null = this.loginService.getCurrentUser();
    if (currentUser && currentUser.email) {
      this.userDisplayName = currentUser.email;
    } else {
      this.userDisplayName = 'User'; // Default if email is not found
    }
  }
} 