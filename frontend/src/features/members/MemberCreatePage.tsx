import { List, Paper, Text } from '@mantine/core';
import { modals } from '@mantine/modals';
import { useState } from 'react';
import type { UseFormSetError } from 'react-hook-form';
import { useNavigate } from 'react-router';
import type { DuplicateMember, MemberInput } from '../../api/types';
import { PageHeader } from '../../components/PageHeader';
import { formatDate } from '../../lib/format';
import { handleFormError, notifySuccess } from '../../lib/forms';
import { MemberForm } from './MemberForm';
import { emptyMember, memberFields } from './memberSchema';
import { checkDuplicates, useMemberMutations } from './membersApi';

function confirmDuplicate(input: MemberInput, duplicates: DuplicateMember[]): Promise<boolean> {
  return new Promise((resolve) => {
    modals.openConfirmModal({
      title: 'Mögliches Duplikat',
      children: (
        <>
          <Text size="sm" mb="xs">
            Es gibt bereits {duplicates.length === 1 ? 'ein Mitglied' : 'Mitglieder'} mit dem Namen{' '}
            <b>
              {input.firstName} {input.lastName}
            </b>{' '}
            und dem Geburtsdatum {formatDate(input.dateOfBirth)}:
          </Text>
          <List size="sm" mb="xs">
            {duplicates.map((d) => (
              <List.Item key={d.id}>
                {d.memberNumber} – {d.firstName} {d.lastName}, {d.city} (
                {d.status === 'Active' ? 'aktiv' : 'inaktiv'})
              </List.Item>
            ))}
          </List>
          <Text size="sm">Trotzdem ein neues Mitglied anlegen?</Text>
        </>
      ),
      labels: { confirm: 'Trotzdem anlegen', cancel: 'Abbrechen' },
      onConfirm: () => resolve(true),
      onCancel: () => resolve(false),
    });
  });
}

/** FR-MEM-01 incl. duplicate warning. */
export function MemberCreatePage() {
  const navigate = useNavigate();
  const { create } = useMemberMutations();
  const [initialValues] = useState(emptyMember);

  const onSubmit = async (input: MemberInput, setError: UseFormSetError<MemberInput>) => {
    try {
      const duplicates = await checkDuplicates(input);
      if (duplicates.length > 0 && !(await confirmDuplicate(input, duplicates))) {
        return;
      }
      const member = await create.mutateAsync({ input, confirmDuplicate: duplicates.length > 0 });
      notifySuccess(`Das Mitglied ${member.memberNumber} wurde angelegt.`);
      navigate(`/mitglieder/${member.id}`);
    } catch (e) {
      handleFormError(e, setError, memberFields);
    }
  };

  return (
    <>
      <PageHeader title="Neues Mitglied" />
      <Paper withBorder p="lg" maw={900}>
        <MemberForm
          initialValues={initialValues}
          submitLabel="Mitglied anlegen"
          onSubmit={onSubmit}
          onCancel={() => navigate('/mitglieder')}
        />
      </Paper>
    </>
  );
}
