import {Directive, Input} from '@angular/core';
import {AbstractControl, NG_VALIDATORS, ValidationErrors, Validator, ValidatorFn} from '@angular/forms';

export function phoneValidator(): ValidatorFn {
    return (control: AbstractControl): {[key: string]: any} | null => {

        const value = control.value.toString().replace('-', '').replace('+', '');

        const forbidden = ((value.length < 9) || (value.length > 10) || !value.match(/^[0-9]+$/));
        return forbidden ? {'invalidPhone': {value: control.value}} : null;
    };
}

@Directive({
    selector: '[phone][formControlName],[phone][formControl],[phone][ngModel]',
    providers: [{provide: NG_VALIDATORS, useExisting: PhoneValidatorDirective, multi: true}]
})
export class PhoneValidatorDirective implements Validator {
    constructor() {
    }

    validate(control: AbstractControl): {[key: string]: any} | null {
        if (!control.value || (control.value.length === 0)) {
            return null;
        }

        return phoneValidator()(control);
    }
}