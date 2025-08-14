 Kanban Tracker – Task List (Clean Architecture, Invites, Board Admins)
0) Solution Setup
 Create solution with projects: Web, Application, Domain, Infrastructure

 Add project references: Web→Application,Infrastructure; Application→Domain; Infrastructure→Application,Domain

 Enable nullable + implicit usings; set C# LangVersion latest

 Register DI in Web: DbContext, Identity, Application services, Authorization policies

1) Database & Identity
 Create ApplicationUser : IdentityUser

 Create AppDbContext : IdentityDbContext<ApplicationUser>

 Add DbSets: Projects, Boards, Columns, Cards, Comments, Labels, CardLabels, ProjectMembers, BoardMembers, Invites

 Add RowVersion (byte[]) to Card (optimistic concurrency)

2) Entities (Domain)
 Project { Id, Name, Key, Description, OwnerId, CreatedAt }

 Board { Id, ProjectId, Name, IsPrivate, CreatedAt }

 Column { Id, BoardId, Name, Order }

 Card { Id, BoardId, ColumnId, Title, Description, Priority(enum), AssigneeId, DueDate?, CreatedAt, UpdatedAt, RowVersion }

 Comment { Id, CardId, AuthorId, Body, CreatedAt }

 Label { Id, ProjectId, Name, Color }

 CardLabel { CardId, LabelId }

 ProjectMember { ProjectId, UserId, Role (Owner|Admin|Member), JoinedAt }

 BoardMember { BoardId, UserId, Role (Admin|Member), AssignedAt }

 Invite { Id, ProjectId, InvitedEmail, InvitedByUserId, Token, ExpiresAt, Status (Pending|Accepted|Revoked) }

3) EF Core Configuration (Infrastructure)
 Fluent API: keys, relationships, cascade rules, required fields

 Unique indexes: ProjectMember(ProjectId,UserId), BoardMember(BoardId,UserId), Label(ProjectId,Name)

 Indexes: Card(BoardId,ColumnId), Card(AssigneeId), Invite(ProjectId,InvitedEmail,Status)

 Query filters (optional): soft delete if you add IsDeleted

 Migrations: Init → Update-Database

4) Seeding
 Seed roles: Admin, User

 Seed 1 platform Admin + 3 demo Users

 Seed 1 Project (owned by you), 2 Boards (one private), 3 Columns per board (To Do/In Progress/Done)

 Seed 6–8 Labels

 Seed ProjectMembers: Owner (you) + 2 Members

 Seed BoardMembers: one Member promoted to Board Admin on the private board

 Seed ~30 Cards with assorted assignees/labels/due dates/comments

5) Authorization Policies
 Register CanAccessProject(projectId) → Platform Admin OR exists ProjectMember

 Register CanManageProject(projectId) → Platform Admin OR ProjectMember.Role ∈ {Owner, Admin}

 Register CanAccessBoard(boardId) → Platform Admin OR (project member & board public) OR BoardMember OR project Owner/Admin

 Register CanManageBoard(boardId) → Platform Admin OR BoardMember.Admin OR project Owner/Admin

 Implement as IAuthorizationHandlers or app-service helpers

6) Application Layer (Services + DTOs + Validation)
 ProjectService

 CreateProject(userId, dto) → adds ProjectMember Owner

 ListProjects(query)

 InviteUser(projectId, email, invitedBy)

 AcceptInvite(token, userId)

 ListMembers(projectId)

 ChangeProjectRole(projectId, memberUserId, newRole) (Owner/Admin; cannot demote last Owner)

 RemoveMember(projectId, memberUserId) (Owner/Admin; cannot remove Owner)

 CreateBoard(projectId, dto) (Owner/Admin)

 BoardService

 GetBoard(boardId) (columns+cards with auth filtering)

 SetBoardPrivacy(boardId, isPrivate) (ManageBoard)

 AddBoardMember(boardId, userId, role) (ManageBoard; must be ProjectMember)

 ChangeBoardRole(boardId, userId, role) (ManageBoard)

 RemoveBoardMember(boardId, userId) (ManageBoard)

 MoveCard(cardId, targetColumnId, targetIndex)

 CardService

 CreateCard(boardId, dto) (Board.Member+; validate assignee is ProjectMember)

 UpdateCard(dto) (Board.Member+)

 Assign(cardId, userId) (must be ProjectMember)

 AddLabel(cardId, labelId) / RemoveLabel(...)

 AddComment(cardId, body, authorId)

 DTOs + FluentValidation for create/update/assign/move/invite

7) Web Layer – Controllers (MVC)
 AccountController → Login, Register, Logout

 ProjectsController

 GET /projects (only user’s projects)

 POST /projects (create; caller becomes Owner)

 GET /projects/{id} (details + boards) → CanAccessProject

 GET /projects/{id}/members → CanManageProject

 POST /projects/{id}/members/{userId}/role → CanManageProject

 DELETE /projects/{id}/members/{userId} → CanManageProject

 POST /projects/{id}/invites (email) → CanManageProject

 InvitesController

 GET /invites/accept?token=... → ProjectService.AcceptInvite

 BoardsController

 POST /projects/{projectId}/boards → CanManageProject

 GET /boards/{id} → CanAccessBoard

 POST /boards/{id}/privacy → CanManageBoard

 GET /boards/{id}/members → CanManageBoard

 POST /boards/{id}/members (userId,role) → CanManageBoard

 POST /boards/{id}/members/{userId}/role → CanManageBoard

 DELETE /boards/{id}/members/{userId} → CanManageBoard

 CardsController

 POST /cards/create → CanAccessBoard + membership checks

 POST /cards/{id}/edit → CanAccessBoard

 POST /cards/{id}/delete → CanManageBoard or card owner (your call)

 POST /cards/{id}/move (AJAX) → CanAccessBoard

 POST /cards/{id}/assign → CanAccessBoard

 CommentsController

 POST /cards/{id}/comments (AJAX) → CanAccessBoard

 LabelsController

 GET /labels?projectId=... → CanAccessProject

 POST /labels/create → CanManageProject

 POST /cards/{id}/labels/{labelId}/add (AJAX) → CanAccessBoard

 POST /cards/{id}/labels/{labelId}/remove (AJAX) → CanAccessBoard

8) Admin Area (Platform Admin)
 Create Area: Admin

 Admin/UsersController

 List users (pagination + search)

 Show platform roles

 POST /admin/users/{id}/make-admin

 POST /admin/users/{id}/remove-admin

 Guard with [Authorize(Policy="IsPlatformAdmin")]

 Navbar: show “Admin” only if user in platform Admin role

9) Views (Razor)
 _Layout.cshtml, _Navbar, _ValidationScriptsPartial

 Account: Login, Register

 Projects: Index (search + pagination), Create, Details (with Boards list + Members + Invites)

 Boards: Details (Kanban)

 Columns grid (To Do / In Progress / Done)

 Card item partial (title, priority, due, assignee avatar, labels)

 Filters: text, assignee, label, priority, due (overdue/this week)

 “Board Members” side panel (add/remove/toggle Admin)

 Privacy toggle (if CanManageBoard)

 Cards: Create/Edit/Details (modal-friendly forms)

 Labels: Index/Create; assign/remove via AJAX on Card details

 Invites: Accept page (handles success/error)

 Admin/Users: Index + role toggle

 Errors: Error/404, Error/500

10) Kanban Interactions
 Implement drag-and-drop with SortableJS (or fallback Move →/← buttons)

 AJAX POST /cards/{id}/move returns updated order/column

 Server-side pagination or “Load more” per column for long lists

 Show loading/spinner and disable buttons during AJAX calls

11) Security & Validation
 [Authorize] globally; [AllowAnonymous] only on Login/Register/Invite Accept

 Policies wired on each endpoint (project/board checks)

 [ValidateAntiForgeryToken] on all POST; include token in AJAX headers

 Encode/escape all user text (no raw HTML for descriptions/comments)

 Validate assignee belongs to project (server-side)

 Concurrency: handle DbUpdateConcurrencyException on Card edit

12) Error Handling & Logging
 UseExceptionHandler("/Error/500"), UseStatusCodePagesWithReExecute("/Error/{0}")

 Friendly 404/500 views

 Log exceptions (Serilog optional)

13) Testing (xUnit)
 ProjectService: CreateProject, InviteUser, AcceptInvite, ChangeProjectRole (prevent demoting last Owner), RemoveMember rules

 BoardService: Add/Change/Remove BoardMember (requires ProjectMember), SetBoardPrivacy, GetBoard respects auth

 CardService: Create/Update/Assign/AddLabel/RemoveLabel/AddComment; MoveCard maintains order across columns

 AuthZ: CanAccessProject/Board, CanManageProject/Board happy/deny paths

 Coverage ≥ 65% for Application layer (generate report)

14) UX Polish & Docs
 Bootstrap 5 responsive; tidy spacing and headings

 Toast notifications (success/error)

 README: setup, env vars, migrations, seed logins, roles, feature list

 3–5 screenshots (Projects, Board, Members, Admin)

 Short demo script (Login → Projects → Board → Move card → Filter → Add comment → Admin)

15) Stretch (only if time allows)
 Live updates with SignalR for card moves/comments

 File attachments on cards

 Transfer project ownership flow

 Soft delete for projects/boards/cards

16) Definition of Done (per feature)
 Unit tests pass (where applicable)

 Server + client validation in place

 AuthZ + AntiForgery enforced

 Builds clean; migrations apply; seed works

 UI responsive; no console errors

 README updated if behavior changed