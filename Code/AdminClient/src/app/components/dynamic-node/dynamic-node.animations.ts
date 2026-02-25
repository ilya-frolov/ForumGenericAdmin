import { 
  trigger, 
  state, 
  style, 
  transition, 
  animate, 
  query, 
  stagger, 
  animateChild,
  keyframes
} from '@angular/animations';

/**
 * Animation definitions for dynamic forms and fields
 */
export const dynamicNodeAnimations = [
  trigger('fieldAnimation', [
    // Fade in when a field appears
    transition(':enter', [
      // style({ opacity: 0, transform: 'translateY(20px) scale(1.5)' }),
      // animate('800ms cubic-bezier(0.68, -0.55, 0.27, 1.55)', style({ opacity: 1, transform: 'translateY(0) scale(1)' }))
    ]),
    // Fade out when a field disappears
    transition(':leave', [
      // style({ opacity: 1, transform: 'translateY(0) scale(1)' }),
      // animate('500ms cubic-bezier(0.55, 0.085, 0.68, 0.53)', style({ opacity: 0, transform: 'translateY(10px) scale(0.95)' }))
    ])
  ]),
  trigger('listAnimation', [
    transition(':enter', [
      query('@fieldAnimation', stagger(40, animateChild()), { optional: true })
    ]),
    transition(':leave', [
      query('@fieldAnimation', stagger(40, animateChild()), { optional: true })
    ])
  ]),
  trigger('repeaterItemAnimation', [
    transition(':enter', [
      // style({ opacity: 0, height: 0, transform: 'translateX(-10px)', overflow: 'hidden' }),
      // animate('800ms cubic-bezier(0.34, 1.56, 0.64, 1)', style({ opacity: 1, height: '*', transform: 'translateX(0)' }))
    ]),
    transition(':leave', [
      // style({ opacity: 1, height: '*', transform: 'translateX(0)' }),
      // animate('500ms cubic-bezier(0.55, 0.085, 0.68, 0.53)', style({ opacity: 0, height: 0, transform: 'translateX(-15px)' }))
    ])
  ]),
  trigger('repeaterAddAnimation', [
    transition(':enter', [
      // style({ opacity: 0, transform: 'scale(0.8)', height: 0 }),
      // animate('800ms cubic-bezier(0.68, -0.6, 0.32, 1.6)', 
      //   keyframes([
      //     style({ opacity: 0, transform: 'scale(0.8)', height: '0px', offset: 0 }),
      //     style({ opacity: 0.5, transform: 'scale(1.05)', height: '*', offset: 0.7 }),
      //     style({ opacity: 1, transform: 'scale(1)', height: '*', offset: 1 })
      //   ])
      // )
    ])
  ]),
  trigger('containerAnimation', [
    transition(':enter', [
      // style({ opacity: 0, transform: 'translateY(30px)' }),
      // animate('800ms cubic-bezier(0.23, 1, 0.32, 1)', style({ opacity: 1, transform: 'translateY(0)' }))
    ]),
    transition(':leave', [
      // style({ opacity: 1, transform: 'translateY(0)' }),
      // animate('500ms cubic-bezier(0.55, 0.085, 0.68, 0.53)', style({ opacity: 0, transform: 'translateY(30px)' }))
    ])
  ]),
  trigger('pulseAnimation', [
    state('active', style({
      boxShadow: '0 0 0 rgba(204,169,44, 0)'
    })),
    transition('* => active', [
      // animate('1500ms ease-in-out', keyframes([
      //   style({ boxShadow: '0 0 0 0 rgba(52, 233, 46, 0.68)', offset: 0 }),
      //   style({ boxShadow: '0 0 0 10px rgba(66, 153, 225, 0)', offset: 0.7 }),
      //   style({ boxShadow: '0 0 0 0 rgba(66, 153, 225, 0)', offset: 1 }),
      // ]))
    ]),
    transition('active => *', [
      // animate('500ms ease-in-out', keyframes([
      //   style({ boxShadow: '0 0 0 10px rgba(219, 21, 21, 0.47)', offset: 0 }),
      //   style({ boxShadow: '0 0 0 0 rgba(187, 27, 16, 0.27)', offset: 0.7 }),
      //   style({ boxShadow: '0 0 0 0 rgba(187, 27, 16, 0)', offset: 1 }),
      // ]))
    ])
  ])
];

/**
 * Animation definitions for the form container
 */
export const formAnimations = [
  trigger('formAnimation', [
    transition(':enter', [
      // style({ opacity: 0, transform: 'translateY(30px)' }),
      // animate('800ms cubic-bezier(0.23, 1, 0.32, 1)', style({ opacity: 1, transform: 'translateY(0)' })),
      query('@*', animateChild(), { optional: true })
    ])
  ]),
  trigger('tabsAnimation', [
    transition(':enter', [
      // style({ opacity: 0, transform: 'translateY(-20px)' }),
      // animate('600ms 200ms cubic-bezier(0.23, 1, 0.32, 1)', style({ opacity: 1, transform: 'translateY(0)' }))
    ])
  ]),
  trigger('buttonsAnimation', [
    transition(':enter', [
      // style({ opacity: 0, transform: 'translateY(20px)' }),
      // animate('600ms 400ms cubic-bezier(0.23, 1, 0.32, 1)', style({ opacity: 1, transform: 'translateY(0)' }))
    ])
  ])
]; 