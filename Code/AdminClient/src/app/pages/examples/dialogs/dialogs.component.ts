import { Component, OnInit } from '@angular/core';

@Component({
    selector: 'app-dialogs',
    templateUrl: './dialogs.component.html',
    standalone: false
})
export class DialogsComponent implements OnInit {
  displayBasicModal: boolean = false;
  displayConfirmModal: boolean = false;
  displayFormModal: boolean = false;
  displayCustomModal: boolean = false;
  displaySidebar: boolean = false;
  
  position: 'top' | 'bottom' | 'left' | 'right' | 'topleft' | 'topright' | 'bottomleft' | 'bottomright' | 'center' = 'center';
  
  constructor() {}
  
  ngOnInit() {
  }
  
  showPositionDialog(position: 'top' | 'bottom' | 'left' | 'right' | 'topleft' | 'topright' | 'bottomleft' | 'bottomright' | 'center') {
    this.position = position;
    this.displayCustomModal = true;
  }
} 