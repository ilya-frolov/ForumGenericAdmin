import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';

/**
 * Log levels for the application
 */
export enum LogLevel {
  OFF = 0,
  ERROR = 1,
  WARN = 2,
  INFO = 3,
  DEBUG = 4,
  TRACE = 5
}

/**
 * Logger configuration interface
 */
export interface LoggerConfig {
  level: LogLevel;
  includeTimestamp: boolean;
}

/**
 * Logger service for consistent logging throughout the application
 * Can be toggled on/off based on environment or runtime configuration
 */
@Injectable({
  providedIn: 'root'
})
export class LoggerService {
  private config: LoggerConfig = {
    level: environment.production ? LogLevel.ERROR : LogLevel.DEBUG,
    includeTimestamp: true
  };

  /**
   * Update logger configuration
   */
  configure(config: Partial<LoggerConfig>): void {
    this.config = { ...this.config, ...config };
  }

  /**
   * Check if a log level is enabled
   */
  isLevelEnabled(level: LogLevel): boolean {
    return level <= this.config.level;
  }

  /**
   * Format message with optional timestamp
   */
  private formatMessage(message: string): string {
    if (this.config.includeTimestamp) {
      const timestamp = new Date().toISOString();
      return `[${timestamp}] ${message}`;
    }
    return message;
  }

  /**
   * Log error message
   */
  error(message: string, ...args: any[]): void {
    if (this.isLevelEnabled(LogLevel.ERROR)) {
      console.error(this.formatMessage(message), ...args);
    }
  }

  /**
   * Log warning message
   */
  warn(message: string, ...args: any[]): void {
    if (this.isLevelEnabled(LogLevel.WARN)) {
      console.warn(this.formatMessage(message), ...args);
    }
  }

  /**
   * Log info message
   */
  info(message: string, ...args: any[]): void {
    if (this.isLevelEnabled(LogLevel.INFO)) {
      console.info(this.formatMessage(message), ...args);
    }
  }

  /**
   * Log debug message
   */
  debug(message: string, ...args: any[]): void {
    if (this.isLevelEnabled(LogLevel.DEBUG)) {
      console.debug(this.formatMessage(message), ...args);
    }
  }

  /**
   * Log trace message
   */
  trace(message: string, ...args: any[]): void {
    if (this.isLevelEnabled(LogLevel.TRACE)) {
      console.trace(this.formatMessage(message), ...args);
    }
  }

  /**
   * Get the current log level
   */
  getLogLevel(): LogLevel {
    return this.config.level;
  }

  /**
   * Set the current log level
   */
  setLogLevel(level: LogLevel): void {
    this.config.level = level;
  }
} 