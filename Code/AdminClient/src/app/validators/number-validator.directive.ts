import {Directive, Input} from '@angular/core';
import {AbstractControl, NG_VALIDATORS, ValidationErrors, Validator, ValidatorFn} from '@angular/forms';

export function numberValidator(): ValidatorFn {
    return (control: AbstractControl): {[key: string]: any} | null => {

        const forbidden = !(control.value.toString().match(/^[-+]?[0-9]\d*\.?[0]*$/));
        return forbidden ? {'invalidNumber': {value: control.value}} : null;
    };
}

@Directive({
    selector: '[appNumberInput]',
    providers: [{provide: NG_VALIDATORS, useExisting: NumberValidatorDirective, multi: true}]
})
export class NumberValidatorDirective implements Validator {
    constructor() {
    }

    validate(control: AbstractControl): {[key: string]: any} | null {
        if ((control.value === undefined) || (control.value === null) || (control.value.length === 0)) {
            return null;
        }

        return numberValidator()(control);
    }
}
