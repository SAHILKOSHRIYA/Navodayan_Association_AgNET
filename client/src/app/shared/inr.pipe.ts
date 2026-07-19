import { Pipe, PipeTransform } from '@angular/core';

/** Formats a number as Indian rupees with the Indian digit grouping (₹1,00,000). */
@Pipe({ name: 'inr', standalone: true })
export class InrPipe implements PipeTransform {
  transform(value: number | null | undefined, withPaise = false): string {
    if (value == null) return '₹0';
    return '₹' + value.toLocaleString('en-IN', {
      minimumFractionDigits: withPaise ? 2 : 0,
      maximumFractionDigits: withPaise ? 2 : 0,
    });
  }
}
