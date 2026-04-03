---
applyTo: "src/**/Generated/*Extensions.cs"
---
# Connector Table Maintenance

When a new connector is added to the `Generated/` folder, update the supported SDK connector names list in `.github/skills/connection-setup/SKILL.md` (Step 2). Add the new connector's API name (e.g., `office365`, `sharepointonline`) to the inline list.

Also update the validated connectors table in `README.md` if the connector has been validated end-to-end.
