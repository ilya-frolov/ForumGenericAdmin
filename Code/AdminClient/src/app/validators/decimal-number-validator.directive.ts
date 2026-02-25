import {Directive, Input} from '@angular/core';
import {AbstractControl, NG_VALIDATORS, ValidationErrors, Validator, ValidatorFn} from '@angular/forms';

export function decimalNumberValidator(): ValidatorFn {
    return (control: AbstractControl): {[key: string]: any} | null => {

        const forbidden = !(control.value.toString().match(/^[-+]?\d+(\.\d+)?$/));
        return forbidden ? {'invalidDecimalNumber': {value: control.value}} : null;
    };
}

@Directive({
    selector: '[appDecimalNumberInput]',
    providers: [{provide: NG_VALIDATORS, useExisting: DecimalNumberValidatorDirective, multi: true}]
})
export class DecimalNumberValidatorDirective implements Validator {
    constructor() {
    }

    validate(control: AbstractControl): {[key: string]: any} | null {
      if ((control.value === undefined) || (control.value === null) || (control.value.length === 0)) {
        return null;
      }

        return decimalNumberValidator()(control);
    }
}
