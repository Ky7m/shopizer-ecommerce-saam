# Admin Design Tokens

## Design source evidence

The legacy visual source is `initial-source/shopizer-admin-main`:

- brand assets: `src/assets/shopizer_logo.svg`, `src/assets/img/shopizer-logo.png`,
  `shopizer-logo-1.png`, provider/payment images (`stripe.png`, `icon-paypal.png`,
  `braintree.jpg`, `beanstream.gif`, `moneyorder.gif`, `paytm.jpg`);
- shipping assets: `canadapost.jpg`, `fedex.jpg`, `purolator.png`, `ups.jpg`, `usps.jpg`;
- utility assets: `noimg.png`, `magnifier.jpg`, `magnifying-glass.png`, `plus.png`,
  `reload.png`, `reset.jpg`;
- layout/theme evidence: `src/app/@theme/styles/{themes.scss,_layout.scss,_overrides.scss,
  custom.scss}`, feature `*.component.scss`, `pages/pages.component.scss`;
- terminology and locale evidence: `src/assets/i18n/en.json`, `es.json`, `fr.json`, `ru.json`,
  plus provider-specific payment/shipping JSON files.

Reuse selected Shopizer/provider images only after asset provenance and license/approval are
recorded for the target distribution. Do not copy Nebular, Bootstrap, TinyMCE, or Angular CSS
wholesale. No Figma file exists in the repository and none is assumed.

## Visual direction

Retain the recognizable Shopizer logo-led auth card, left admin navigation, page title/header,
card-and-table information grouping, right-side detail menu, provider icons, and labels.
Modernize with a restrained neutral canvas, high-contrast text, consistent focus rings,
semantic controls, responsive table/card transitions, reduced motion, and usable touch targets.

## Tokens

| Token | Value/intent |
|---|---|
| `--admin-color-brand` | approved Shopizer logo accent; derive from approved asset, do not sample an unapproved provider logo |
| `--admin-color-surface` | white or near-white content cards |
| `--admin-color-canvas` | cool light neutral replacing the legacy `#e3e9ee` background |
| `--admin-color-text` | near-black with WCAG AA contrast |
| `--admin-color-muted` | secondary text with AA contrast for normal-size text |
| `--admin-color-success/warning/danger/info` | semantic status colors; never color-only |
| `--admin-space-1..8` | 4px base scale: 4, 8, 12, 16, 24, 32, 48, 64px |
| `--admin-radius-sm/md/lg` | 4, 8, 12px; use consistent cards/dialogs |
| `--admin-shadow-card` | subtle elevation, not a layout dependency |
| `--admin-font-body` | system UI stack with local fallback |
| `--admin-font-size` | 14-16px body, 28-32px page title, responsive downshift |
| `--admin-focus` | 2px visible outline with offset and brand-compatible contrast |
| `--admin-touch-target` | minimum 44px interactive target |

## Responsive and accessibility rules

- Breakpoints are content-driven: desktop navigation, tablet collapsible navigation, and
  narrow mobile drawer; do not require a fixed 850px content width from the legacy image
  manager styles.
- Tables expose a mobile card representation with the same fields and actions.
- All dialogs trap focus, restore focus to the trigger, close via Escape when safe, and have a
  programmatic label.
- Form errors are associated with controls and summarized; pending states are announced and
  submit controls prevent duplicate requests.
- Keyboard navigation covers menu tree, tabs, tables, hierarchy moves, file picker, and image
  selection. Drag/drop always has a keyboard alternative.
- Status uses text/icon/structure in addition to color. Respect `prefers-reduced-motion`.
- Logo/provider assets have meaningful alt text when informative and empty alt when decorative.
- Rich text is sanitized according to an approved implementation boundary; raw HTML is never
  trusted because it originated in the legacy editor.
