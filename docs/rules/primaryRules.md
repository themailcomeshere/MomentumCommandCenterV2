# Project Hard Rules

You are an expert software architect and context engineer.

Analyze this repository and generate a set of rules files that help AI coding agents understand and work effectively within the project.

Before writing rules, explore the repository thoroughly. Review READMEs, directory structures, configuration files, and representative code. Look for patterns in the code to infer architecture, conventions, and workflows.

Investigate:
- overall architecture and major components
- folder structure and module responsibilities
- languages, frameworks, and major libraries
- dependency management and build systems
- how the project is run locally
- how the project is built
- how tests are written and executed
- coding conventions and patterns in the code
- commenting style
- variable, function, and file naming conventions
- linting, formatting, and tooling
- infrastructure or environment configuration

Infer conventions from the codebase whenever possible.

Generate rules files inside `docs/rules`.

Create a **primary rules file** that should be included in every prompt. It should contain:
- a short overview of the project
- the repository structure
- key architectural principles
- core frameworks/dependencies
- instructions for building, running, and testing
- a list of important rules written with clear directives such as ALWAYS and NEVER.

Create additional **task-specific or technology-specific rules files** when appropriate (for example testing, language-specific patterns, infrastructure, or build rules).

Keep rules concise, explicit, and derived from the repository. Prefer directive statements like:

ALWAYS follow the existing module structure  
ALWAYS use the project's testing framework  
NEVER introduce new frameworks without justification

Avoid vague advice. Focus on rules that help an AI agent modify the codebase safely and consistently.


1. **Source of Truth:** The file `primaryRules.md` is the primary authority. If a user request conflicts with it, point out the conflict immediately.
2. **No Fabrication:** Unless you are creating Dummy Test Data to be used with Unit Testing or a Test-Only file, never invent statistics, names, dates, or financial figures. If data is missing explicitly state: "Data not found in sources."
3. **Uncertainty Flagging:** At the end of any complex response, add a short section titled `[Uncertainties]` listing any assumptions you had to make.
4. **Tone Enforcement:** Keep language direct, concise, and free of marketing hype or filler openings (e.g., avoid "In today's fast-paced world...").
5. **Prioritize Thoroughness:** The goal is to make the best software we can. Do not deliver incomplete work or fill in the blanks with best estimates. Your analysis, data retrieval, tasks, and all of your work is the most important thing and you are to fulfill it. I will not be impatient if you take a minute, 5 minutes, 20 minutes, or more. Tell me and update me throughout if it will take longer than 30 seconds (e.g. "This request requires a lot... it will take several minutes").


### AI Assistant Guidelines
**ALWAYS self-reflect after a correction is made by the user, review rules, and attempt to add any additional rules**

When a user makes a correction:
1. **Self-Reflect**: Analyze what went wrong and why the mistake occurred
2. **Review Existing Rules**: Check if current rules adequately covered the situation
3. **Identify Gaps**: Determine if there were missing guidelines or unclear instructions
4. **Evolve the Rule System**: Proactively suggest or add new rules, clarifications, or improvements
5. **Document Learnings**: Update relevant rule files to prevent similar issues

This creates a continuous improvement feedback loop where:
- User corrections become learning opportunities
- The rule system evolves and becomes more comprehensive
- Common mistakes get systematically prevented
- AI assistance becomes more aligned with team expectations


# Development Rules

This directory is the canonical home for repository-wide engineering rules and coding conventions.

- The solution currently uses conventional .NET 8 Core, Application, Infrastructure, CLI, and test projects; it is not an ABP Framework application. Apply ABP-specific instructions only if that architecture is deliberately adopted later.
- Use Modern .NET "3 Slashes" XML Flower Box flowerboxing on every class/type, constructor, and method. State what it does and when it is called.
- Preserve clean layer boundaries, deterministic/testable calculations, explicit data freshness, no-look-ahead replay, and source-study parity testing.
- Put future rule documents in this directory and link them here to keep one discoverable source of truth.
