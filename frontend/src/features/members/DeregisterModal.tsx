import { zodResolver } from '@hookform/resolvers/zod';
import { Button, Group, Modal, Stack, Text, Textarea } from '@mantine/core';
import { Controller, useForm } from 'react-hook-form';
import { z } from 'zod';
import type { Member } from '../../api/types';
import { DateField } from '../../components/DateField';
import { todayIso } from '../../lib/format';
import { handleFormError, notifySuccess } from '../../lib/forms';
import { optionalText } from '../../lib/schemas';
import { useMemberMutations } from './membersApi';

const schema = z.object({
  leaveDate: z.string().min(1, 'Bitte das Austrittsdatum eingeben.'),
  reason: optionalText(500),
});
type FormValues = z.infer<typeof schema>;

/** FR-MEM-05: confirmation dialog with leave date (default today) and optional reason. */
export function DeregisterModal({
  member,
  opened,
  onClose,
}: {
  member: Member;
  opened: boolean;
  onClose: () => void;
}) {
  const { deregister } = useMemberMutations();
  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { leaveDate: todayIso(), reason: '' },
  });

  const onSubmit = handleSubmit(async (values) => {
    try {
      await deregister.mutateAsync({ id: member.id, ...values });
      notifySuccess('Das Mitglied wurde abgemeldet.');
      onClose();
    } catch (e) {
      handleFormError(e, setError, ['leaveDate', 'reason']);
    }
  });

  return (
    <Modal opened={opened} onClose={onClose} title="Mitglied abmelden">
      <form onSubmit={onSubmit} noValidate>
        <Stack>
          <Text size="sm">
            <b>
              {member.firstName} {member.lastName}
            </b>{' '}
            wird auf inaktiv gesetzt. Zahlungen und Rechnungen bleiben erhalten; neue Zahlungen sind danach
            nicht mehr möglich.
          </Text>
          <Controller
            control={control}
            name="leaveDate"
            render={({ field }) => (
              <DateField
                label="Austrittsdatum"
                withAsterisk
                value={field.value}
                onChange={field.onChange}
                error={errors.leaveDate?.message}
              />
            )}
          />
          <Textarea
            label="Grund"
            autosize
            minRows={2}
            {...register('reason')}
            error={errors.reason?.message}
          />
          <Group justify="flex-end">
            <Button variant="default" onClick={onClose}>
              Abbrechen
            </Button>
            <Button type="submit" color="red" loading={isSubmitting}>
              Abmelden
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  );
}
