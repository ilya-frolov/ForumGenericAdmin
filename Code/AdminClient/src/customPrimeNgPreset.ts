// customPrimeNgPreset.ts

import { definePreset, palette } from '@primeng/themes';
import Aura from '@primeng/themes/aura';

// customPrimaryColor
const customLightSurface = palette('#dbe5ec');
const customDarkSurface = palette('#040522');
const customPrimary = palette('#3dd232');
const customSecondary = palette('#085e49');

const customPrimeNgPreset = definePreset(Aura, {
  components: {
    // Styling example
    // button: {
    //   colorScheme: {
    //     light: {
    //       secondary: {
    //         root: {
    //           color: '#ffffff',
    //           background: customSecondary[400],
    //           borderColor: customSecondary[400],
    //           hoverColor: '#ffffff',
    //           hoverBackground: customSecondary[500],
    //           hoverBorderColor: customSecondary[500],
    //           activeColor: '#ffffff',
    //           activeBackground: customSecondary[600],
    //           activeBorderColor: customSecondary[600],
    //           focusColor: '#ffffff',
    //           focusBackground: customSecondary[500],
    //           focusBorderColor: customSecondary[500],
    //         }
    //       }
    //     }
    //   }
    // }
  },
  semantic: {
    primary: customPrimary,
    secondary: customSecondary,
    colorScheme: {
      light: {
        //surface: customLightSurface
        surface: {
            0: '#ffffff',
            50: '{zinc.50}',
            100: '{zinc.100}',
            200: '{zinc.200}',
            300: '{zinc.300}',
            400: '{zinc.400}',
            500: '{zinc.500}',
            600: '{zinc.600}',
            700: '{zinc.700}',
            800: '{zinc.800}',
            900: '{zinc.900}',
            950: '{zinc.950}'
        }
      },
      dark: {
        //surface: customDarkSurface
        surface: {
            0: '#ffffff',
            50: '{zinc.50}',
            100: '{zinc.100}',
            200: '{zinc.200}',
            300: '{zinc.300}',
            400: '{zinc.400}',
            500: '{zinc.500}',
            600: '{zinc.600}',
            700: '{zinc.700}',
            800: '{zinc.800}',
            900: '{zinc.900}',
            950: '{zinc.950}'
        }
      }
    }
  }
});

export default customPrimeNgPreset;