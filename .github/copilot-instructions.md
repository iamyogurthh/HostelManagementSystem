# Copilot Instructions

## Project Guidelines
- When requesting project documentation or diagrams, the user prefers the result saved as a Markdown (.md) file in the workspace.
- Revert the recently applied 140x48 standard button sizing; do not retain that sizing preference as the current UI standard.
- All application buttons should preserve 32x32 for compact DataGrid icon buttons. The shared button styles should define the exact same height and minimum width where appropriate.
- For the application startup splash window, use a simple visual loading screen without status text or a progress bar.

## Billing Preferences
- Use only manually entered charges; an administrator specifies the student, date range, and total amount.
- A manual charge overrides any previous billing for its selected date range and is represented as one total charge across room changes.
- Allow administrators to either type a total amount or select an existing fee rule.
- Student selection must scale efficiently for thousands of students, preferably with searchable/server-filtered selection rather than loading every student into a ComboBox.
- The manual fee charge form should match the existing application form layout and styling.

## Software Distribution
- For software distribution, use a public GitHub repository with GitHub Actions, Inno Setup, and GitHub Releases.
- Implement the release pipeline step by step, starting with automated installer creation.
- For application updates, implement the simple first version: asynchronously check the latest stable GitHub Release once at startup, notify users when a newer version exists, and let them open/download the installer manually; do not implement automatic installation yet.