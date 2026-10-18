import { Badge, NumberInput, type NumberInputProps } from '@mantine/core';
import {
  emailStatusLabels,
  invoiceStatusLabels,
  paymentStatusLabels,
  type EmailStatus,
  type InvoiceStatus,
  type PaymentStatus,
} from '../api/types';

export function InvoiceStatusBadge({ status }: { status: InvoiceStatus }) {
  return (
    <Badge color={status === 'Issued' ? 'teal' : 'red'} variant="light">
      {invoiceStatusLabels[status]}
    </Badge>
  );
}

export function PaymentStatusBadge({ status }: { status: PaymentStatus }) {
  return (
    <Badge color={status === 'Recorded' ? 'teal' : 'red'} variant="light">
      {paymentStatusLabels[status]}
    </Badge>
  );
}

const emailColors: Record<EmailStatus, string> = {
  NotApplicable: 'gray',
  Pending: 'yellow',
  Sent: 'teal',
  Failed: 'red',
};

export function EmailStatusBadge({ status }: { status: EmailStatus }) {
  return (
    <Badge color={emailColors[status]} variant="light">
      {emailStatusLabels[status]}
    </Badge>
  );
}

/** EUR amount input in German notation ("1.234,56 €"). */
export function MoneyInput(props: NumberInputProps) {
  return (
    <NumberInput
      decimalSeparator=","
      thousandSeparator="."
      decimalScale={2}
      fixedDecimalScale
      min={0}
      suffix=" €"
      hideControls
      {...props}
    />
  );
}
