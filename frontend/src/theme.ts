import { createTheme, type CSSVariablesResolver } from '@mantine/core';

export const theme = createTheme({
  primaryColor: 'teal',
  // teal.9 keeps white button text above 4.5:1 (NFR-UX-05, WCAG 2.1 AA).
  primaryShade: { light: 9, dark: 7 },
  defaultRadius: 'sm',
  fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto, sans-serif',
});

/**
 * Contrast fixes for the light scheme (checked with axe-core):
 * - Mantine's default "dimmed" gray is ~3.3:1 on white; this one is ~5.6:1.
 * - "light" variants (badges, active nav link, avatar) get the lightest background so their text reaches 4.5:1;
 *   yellow text needs a custom dark shade.
 */
export const cssVariablesResolver: CSSVariablesResolver = () => ({
  variables: {},
  light: {
    '--mantine-color-dimmed': '#5f676f',
    '--mantine-color-teal-light': 'var(--mantine-color-teal-0)',
    '--mantine-color-red-light': 'var(--mantine-color-red-0)',
    '--mantine-color-gray-light': 'var(--mantine-color-gray-1)',
    '--mantine-color-yellow-light': 'var(--mantine-color-yellow-0)',
    '--mantine-color-yellow-light-color': '#7a4a00',
  },
  dark: {},
});
