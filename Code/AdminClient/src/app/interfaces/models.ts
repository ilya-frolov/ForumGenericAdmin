export interface InitData {
    allowHebrew: boolean;
    allowEnglish: boolean;
    newServerUrl: string;
}

export enum IconType {
    FontAwesome = 1,
    PrimeIcons = 2
}

export interface AdminSegment {
    navigation: AdminSegmentNavigation;
    ui: AdminSegmentUI;
    general: AdminSegmentGeneral;
}

export interface AdminSegmentNavigation {
    controllerName: string;
    customPath: string;
    referenceColumn: string;
}

export interface AdminSegmentUI {
    icon: string;
    iconType: IconType;
    iconFamily: string;
    showInMenu: boolean;
}

export interface AdminSegmentGeneral {
    id: string;
    name: string;
    isGeneric: boolean;
    isSettings: boolean;
    priority: number;
    menuHeader?: string; // Optional header for grouping segments under custom submenu headers
}

export interface AdminSettingsSegment {
    id: string;
    name: string;
    controllerName: string;
}

export interface AdminHomeData {
    showDashboard: boolean;
    showGlobalStatistics: boolean;
    siteBaseHref: string;
    userName: string;
    segments: AdminSegment[];
    settings: AdminSettingsSegment[];
}


export interface ConnectedUser {
    email: string;
    // role: string;
}
