import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LoggerService, LogLevel } from '../../services/logger.service';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-log-level-control',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './log-level-control.component.html',
  styleUrls: ['./log-level-control.component.scss']
})
export class LogLevelControlComponent implements OnInit {
  logLevels = [
    { name: 'Off', value: LogLevel.OFF },
    { name: 'Error', value: LogLevel.ERROR },
    { name: 'Warning', value: LogLevel.WARN },
    { name: 'Info', value: LogLevel.INFO },
    { name: 'Debug', value: LogLevel.DEBUG },
    { name: 'Trace', value: LogLevel.TRACE }
  ];

  selectedLevel: LogLevel = LogLevel.DEBUG;

  constructor(private loggerService: LoggerService) { }

  ngOnInit(): void {
    // Initialize the selected level from the service
    // We could store this in localStorage for persistence
    this.selectedLevel = this.getCurrentLogLevel();
  }

  changeLogLevel(): void {
    this.loggerService.setLogLevel(this.selectedLevel);
    this.loggerService.info(`Log level changed to ${this.getLogLevelName(this.selectedLevel)}`);
  }

  getCurrentLogLevel(): LogLevel {
    return this.loggerService.getLogLevel();
  }

  getLogLevelName(level: LogLevel): string {
    const found = this.logLevels.find(l => l.value === level);
    return found ? found.name : 'Unknown';
  }
} 