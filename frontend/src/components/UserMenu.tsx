import { Avatar, Group, Menu, Text, UnstyledButton } from '@mantine/core';
import { IconKey, IconLogout } from '@tabler/icons-react';
import { useNavigate } from 'react-router';
import { roleLabels } from '../api/types';
import { useAuth } from '../auth/useAuth';

export function UserMenu() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  if (!user) return null;

  const onLogout = async () => {
    await logout();
    navigate('/login', { replace: true });
  };

  return (
    <Menu position="bottom-end" withinPortal>
      <Menu.Target>
        <UnstyledButton aria-label="Benutzermenü">
          <Group gap="xs">
            <Avatar color="teal" radius="xl" size="sm">
              {user.firstName[0]}
              {user.lastName[0]}
            </Avatar>
            <div>
              <Text size="sm" fw={500} lh={1.2}>
                {user.firstName} {user.lastName}
              </Text>
              <Text size="xs" c="dimmed" lh={1.2}>
                {roleLabels[user.role]}
              </Text>
            </div>
          </Group>
        </UnstyledButton>
      </Menu.Target>
      <Menu.Dropdown>
        <Menu.Item leftSection={<IconKey size={16} />} onClick={() => navigate('/passwort-aendern')}>
          Passwort ändern
        </Menu.Item>
        <Menu.Item leftSection={<IconLogout size={16} />} onClick={onLogout}>
          Abmelden
        </Menu.Item>
      </Menu.Dropdown>
    </Menu>
  );
}
