import { Anchor, Badge, Button, Grid, Group, Modal, Paper, Stack, Text, Title } from '@mantine/core';
import { IconArrowLeft } from '@tabler/icons-react';
import { useState, type ReactNode } from 'react';
import type { UseFormSetError } from 'react-hook-form';
import { Link, useParams } from 'react-router';
import { intervalLabels, memberStatusLabels, type Member, type MemberInput } from '../../api/types';
import { useHasRole } from '../../auth/useAuth';
import { ErrorState, LoadingState } from '../../components/common';
import { confirmAction } from '../../components/confirm';
import { PageHeader } from '../../components/PageHeader';
import { formatDate, formatDateTime } from '../../lib/format';
import { handleFormError, notifyError, notifySuccess } from '../../lib/forms';
import { DeregisterModal } from './DeregisterModal';
import { MemberForm } from './MemberForm';
import { memberFields, toMemberInput } from './memberSchema';
import { useMember, useMemberMutations } from './membersApi';

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div>
      <Text size="xs" c="dimmed" tt="uppercase" fw={600}>
        {label}
      </Text>
      <Text>{children || '–'}</Text>
    </div>
  );
}

/** FR-MEM-03 details; FR-MEM-04..06 actions for Admin/Secretary. */
export function MemberDetailPage() {
  const { id } = useParams();
  const { data: member, isLoading, error } = useMember(id);
  const canWrite = useHasRole('Admin', 'Secretary');
  const [editing, setEditing] = useState(false);
  const [deregistering, setDeregistering] = useState(false);
  const { update, reactivate } = useMemberMutations();

  if (isLoading) return <LoadingState />;
  if (error || !member) return <ErrorState error={error} />;

  const onEdit = async (input: MemberInput, setError: UseFormSetError<MemberInput>) => {
    try {
      await update.mutateAsync({ id: member.id, input, rowVersion: member.rowVersion });
      notifySuccess('Die Änderungen wurden gespeichert.');
      setEditing(false);
    } catch (e) {
      handleFormError(e, setError, memberFields);
    }
  };

  const onReactivate = () =>
    confirmAction({
      title: 'Mitglied reaktivieren',
      message: `${member.firstName} ${member.lastName} wird wieder auf aktiv gesetzt.`,
      confirmLabel: 'Reaktivieren',
      danger: false,
      onConfirm: async () => {
        try {
          await reactivate.mutateAsync(member.id);
          notifySuccess('Das Mitglied wurde reaktiviert.');
        } catch (e) {
          notifyError(e);
        }
      },
    });

  return (
    <>
      <Anchor component={Link} to="/mitglieder" size="sm">
        <Group gap={4}>
          <IconArrowLeft size={14} /> Zurück zur Liste
        </Group>
      </Anchor>
      <PageHeader
        title={`${member.firstName} ${member.lastName}`}
        actions={
          canWrite && (
            <>
              <Button variant="default" onClick={() => setEditing(true)}>
                Bearbeiten
              </Button>
              {member.status === 'Active' ? (
                <Button color="red" variant="light" onClick={() => setDeregistering(true)}>
                  Abmelden
                </Button>
              ) : (
                <Button variant="light" onClick={onReactivate}>
                  Reaktivieren
                </Button>
              )}
            </>
          )
        }
      />

      <Stack>
        <MemberFields member={member} />
      </Stack>

      <Modal opened={editing} onClose={() => setEditing(false)} title="Mitglied bearbeiten" size="xl">
        <MemberForm
          initialValues={toMemberInput(member)}
          submitLabel="Speichern"
          onSubmit={onEdit}
          onCancel={() => setEditing(false)}
        />
      </Modal>
      <DeregisterModal member={member} opened={deregistering} onClose={() => setDeregistering(false)} />
    </>
  );
}

function MemberFields({ member }: { member: Member }) {
  return (
    <Paper withBorder p="lg">
      <Group justify="space-between" mb="md">
        <Title order={4}>Stammdaten</Title>
        <Group gap="xs">
          <Badge variant="outline">{member.memberNumber}</Badge>
          <Badge color={member.status === 'Active' ? 'teal' : 'gray'} variant="light">
            {memberStatusLabels[member.status]}
          </Badge>
        </Group>
      </Group>
      <Grid>
        <Grid.Col span={{ base: 12, sm: 4 }}>
          <Field label="Geburtsdatum">{formatDate(member.dateOfBirth)}</Field>
        </Grid.Col>
        <Grid.Col span={{ base: 12, sm: 8 }}>
          <Field label="Adresse">
            {member.street}, {member.postalCode} {member.city}, {member.country}
          </Field>
        </Grid.Col>
        <Grid.Col span={{ base: 12, sm: 4 }}>
          <Field label="E-Mail">{member.email}</Field>
        </Grid.Col>
        <Grid.Col span={{ base: 12, sm: 4 }}>
          <Field label="Telefon">{member.phone}</Field>
        </Grid.Col>
        <Grid.Col span={{ base: 12, sm: 4 }}>
          <Field label="Zahlungsintervall">{intervalLabels[member.billingInterval]}</Field>
        </Grid.Col>
        <Grid.Col span={{ base: 12, sm: 4 }}>
          <Field label="Eintrittsdatum">{formatDate(member.joinDate)}</Field>
        </Grid.Col>
        {member.status === 'Inactive' && (
          <>
            <Grid.Col span={{ base: 12, sm: 4 }}>
              <Field label="Austrittsdatum">{formatDate(member.leaveDate)}</Field>
            </Grid.Col>
            <Grid.Col span={{ base: 12, sm: 4 }}>
              <Field label="Austrittsgrund">{member.leaveReason}</Field>
            </Grid.Col>
          </>
        )}
        <Grid.Col span={12}>
          <Field label="Notizen">{member.notes}</Field>
        </Grid.Col>
        <Grid.Col span={12}>
          <Text size="xs" c="dimmed">
            Angelegt am {formatDateTime(member.createdAt)} · Zuletzt geändert am{' '}
            {formatDateTime(member.updatedAt)}
          </Text>
        </Grid.Col>
      </Grid>
    </Paper>
  );
}
