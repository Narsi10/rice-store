# Agile & Scrum — Interview Q&A

Covers work item types, ceremonies, roles, and common questions. Useful for the
"working with business clients / dynamic environment" parts of the JD.

## Contents
1. Agile fundamentals
2. Work item / issue types (the hierarchy)
3. Scrum roles
4. Scrum ceremonies (events)
5. Scrum artifacts
6. Common Q&A
7. Rapid-fire one-liners

---

## 1. Agile fundamentals

**Q: What is Agile?**
Agile is an iterative approach to delivering software in small, frequent
increments rather than one big release. Work is done in short cycles, with
continuous feedback from the business so the product adapts to changing needs.
Core values (from the Agile Manifesto): individuals and interactions, working
software, customer collaboration, and responding to change.

**Q: Agile vs Waterfall?**
Waterfall is sequential — requirements → design → build → test → release, all
planned up front, with change being expensive. Agile is iterative — you build a
little, get feedback, and adjust every sprint, so it handles changing requirements
far better.

**Q: What is Scrum?**
Scrum is the most popular Agile framework. Work is delivered in fixed-length
**sprints** (usually 2 weeks), with defined roles, ceremonies, and artifacts to
keep the team aligned and delivering.

---

## 2. Work item / issue types (the hierarchy)

From biggest to smallest:

| Type | Size | Answers | Example |
|------|------|---------|---------|
| **Epic** | Very large (many sprints) | What big goal? | "Online Payment System" |
| **Feature** | Large | What capability? | "Support UPI payments" |
| **User Story** | Fits one sprint | What does the user need? | "As a customer, I want to pay by UPI so I can check out quickly" |
| **Task** | Hours/days | What technical work? | "Build the payment API endpoint" |
| **Sub-task** | Smallest | Finer breakdown | "Add UPI ID validation" |
| **Bug/Defect** | Varies | What's broken? | "UPI fails for amounts over Rs.10,000" |

```
Epic
  └── Feature
        └── User Story   (one sprint)
              └── Task
                    └── Sub-task
Bug — tracked separately, usually at Story/Task level
```

**Q: What is a User Story and its format?**
A small, user-focused requirement completable in one sprint. Format:
*"As a [user], I want [goal] so that [benefit]."* It focuses on the value to the
user, not the technical implementation.

**Q: What are acceptance criteria?**
The conditions a story must meet to be considered done — the checklist that defines
"working." They make the story testable and remove ambiguity.

**Q: What is "Definition of Done" (DoD)?**
A shared checklist the team agrees on for when *any* work item is truly complete —
e.g. code reviewed, tests passing, merged, deployed to a test environment,
documented. Prevents "done but not really done."

---

## 3. Scrum roles

**Product Owner (PO)** — owns the product backlog, prioritizes what gets built,
represents the business/customer, and defines acceptance criteria. Decides *what*
and *why*.

**Scrum Master** — facilitates the process, removes blockers, coaches the team on
Scrum, protects the team from distractions. Serves the team; not a "boss."

**Development Team** — the people who build the product (developers, testers, etc.).
Self-organizing; decides *how* the work gets done.

---

## 4. Scrum ceremonies (events)

**Sprint Planning** — at the start of a sprint, the team decides what to commit to
from the backlog and how they'll do it. Produces the sprint backlog.

**Daily Standup (Daily Scrum)** — a short (15-min) daily sync. Each person covers:
what I did yesterday, what I'll do today, any blockers. Keeps everyone aligned and
surfaces impediments early.

**Sprint Review** — at the end of the sprint, the team demos the completed work to
stakeholders and gets feedback. Focused on the product.

**Sprint Retrospective** — after the review, the team reflects on the *process*:
what went well, what didn't, what to improve next sprint. Focused on the team.

**Backlog Refinement (Grooming)** — ongoing session to clarify, estimate, and
prioritize upcoming backlog items so they're ready for future sprints.

---

## 5. Scrum artifacts

**Product Backlog** — the master prioritized list of everything the product might
need, owned by the PO.

**Sprint Backlog** — the subset of items the team committed to for the current
sprint, plus the plan to deliver them.

**Increment** — the working, potentially shippable product produced at the end of
each sprint.

**Burndown chart** — shows remaining work vs time in the sprint, so the team can
see if they're on track.

---

## 6. Common Q&A

**Q: What is story point estimation?**
Estimating the relative effort/complexity of a story rather than exact hours, often
using a Fibonacci-like scale (1, 2, 3, 5, 8, 13). Teams frequently use **Planning
Poker** to agree on points. It focuses on relative size, which is more reliable
than guessing hours.

**Q: What is velocity?**
The average number of story points a team completes per sprint. Used to forecast
how much the team can take on in future sprints.

**Q: What is a sprint and how long is it?**
A fixed time-box (usually 1–4 weeks, commonly 2) in which the team delivers a
working increment. Length stays consistent so the team develops a predictable
rhythm.

**Q: How do you handle changing requirements mid-sprint?**
The sprint scope is meant to be protected — ideally changes wait for the next
sprint. If something is truly urgent, the PO and team discuss trade-offs; adding
work usually means removing something else. Genuinely urgent items may justify
re-planning, but frequent mid-sprint changes are a red flag.

**Q: What's the difference between Agile and Scrum?**
Agile is the *mindset/philosophy* (iterative, feedback-driven). Scrum is a specific
*framework* that implements Agile with defined roles, events, and artifacts. Kanban
is another Agile framework.

**Q: What is Kanban vs Scrum?**
Kanban is a continuous-flow approach using a board with WIP (work-in-progress)
limits, no fixed sprints. Scrum works in time-boxed sprints. Kanban suits
support/ops work with a steady stream of incoming tasks; Scrum suits planned
feature delivery.

**Q: (For a support role) How does Agile apply to production support?**
Support/ops teams often use **Kanban** rather than sprints, because incidents
arrive unpredictably. A board tracks incidents through states (New → In Progress →
Resolved) with WIP limits, and retrospectives still drive continuous improvement.

---

## 7. Rapid-fire one-liners

- **Epic → Feature → Story → Task → Sub-task** = work item hierarchy, big to small.
- **Bug** = a defect, tracked separately.
- **User Story format** = "As a [user], I want [goal] so that [benefit]."
- **PO** = what/why (backlog & priorities). **Scrum Master** = process/blockers.
  **Dev team** = how.
- **Ceremonies** = Planning, Daily Standup, Review, Retrospective, Refinement.
- **Artifacts** = Product Backlog, Sprint Backlog, Increment, Burndown.
- **Story points** = relative effort (Fibonacci); **velocity** = points/sprint.
- **Review** = demo the product; **Retro** = improve the process.
- **Scrum** = time-boxed sprints; **Kanban** = continuous flow with WIP limits.
- **DoD** = shared checklist for "truly done."

---

## Interview tip

For a DevOps/support role, connect Agile to your daily work: "We tracked our work
as stories and tasks in Azure Boards / Jira, joined the daily standup to surface
blockers, and used retrospectives to improve our process — for support incidents we
leaned toward a Kanban flow since they arrive unpredictably." That shows you've
lived it, not just memorized definitions.
