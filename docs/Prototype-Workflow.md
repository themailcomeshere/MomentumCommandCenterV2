# Dashboard Production / Prototype workflow

- `frontend/index.html` is the production dashboard entry point and remains unchanged during UI experiments.
- `frontend/prototypeIndex.html` is the comparison/prototype entry point, accessible at `/prototypeIndex.html` from the local API host.
- Every future dashboard UI change is applied to `prototypeIndex.html` first. Shared JavaScript changes are allowed only when they preserve production behavior and are required for both layouts to work.
- When the user approves a prototype, copy only the approved UI changes into `index.html` and shared styles/assets as appropriate. Do not promote automatically.
- Compare root `/` (production) with `/prototypeIndex.html` (prototype) in separate browser tabs.
- The prototype workspace switch is a presentation control that maps to the existing `#mode` application state. The symbol text field maps to the existing `#symbol` focus state.
- The prototype is not a separate trading engine or API instance. Both pages use the same local API and backend data.
