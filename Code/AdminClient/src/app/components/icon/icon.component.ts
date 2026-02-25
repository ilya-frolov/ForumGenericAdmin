import {Component, Input} from '@angular/core';
import {CommonModule} from "@angular/common";
import {IconType} from "../../interfaces/models";

@Component({
  selector: 'app-icon',
  standalone: false,
  templateUrl: './icon.component.html'
})
export class IconComponent {
  @Input() iconType: IconType;
  @Input() iconName: string;
  protected readonly IconType = IconType;
}
