import { Directive, HostListener } from '@angular/core';

@Directive({
  selector: '[appDigitsOnly]',
  standalone: true,
})
export class DigitsOnlyDirective {
  @HostListener('beforeinput', ['$event'])
  onBeforeInput(event: InputEvent) {
    // Before input is to prevent pasting numbers also, not only keystroke
    if (event.data && !/^[0-9]*$/.test(event.data)) {
      event.preventDefault();
    }
  }
}
