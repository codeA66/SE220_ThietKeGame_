# Gothic Fortress vòng 2 — Implementation Plan

> **For agentic workers:** Thực thi bằng **Antigravity native Swarm** theo lựa chọn của người dùng. Mỗi checkout có một orchestrator; worker nhận task/file lease cụ thể và reviewer chỉ đọc. Không thay phương thức này bằng một agent làm toàn bộ hoặc yêu cầu cài framework orchestration khác.

**Goal:** Sửa các finding đã xác minh, chuẩn hóa cả hai checkout về Unity **6000.6.9f1**, hoàn tất authoring và runtime/integration theo FORTRESS-1, xuất bằng chứng mới để người dùng xét nghiệm thu.

**Architecture:** Giữ hai repository/nhánh hiện có và ownership theo đầu ra. Antigravity chạy cả hai swarm: Authoring sở hữu presentation và commit migration chung; Runtime sở hữu gameplay môi trường và tích hợp cuối. Trao đổi bằng report và commit đóng băng; không sửa chéo checkout.

**Tech Stack:** Unity 6000.6.9f1; C#; URP 17.6.0; Tilemap Extras 9.0.1; Input System 1.20.1; Test Framework 1.8.0; Pipeline 1.0.0; Unity CLI/plugin; RTK; Antigravity Swarm.

**Spec:** Yêu cầu người dùng ngày 16/11/2026 và review trước đó; `docs/Fortress/Tasks.md` (toàn bộ C00–C14/A00–A13), `# MASTER PROMPT — COMPLETE UNITY 2D.md` mục 0–28. Hai goal vòng 2: `docs/Fortress/Goal-Round2-Authoring.md`, `docs/Fortress/Goal-Round2-Runtime.md`.

## 1. Global Constraints và quyền của vòng 2

- Lượt soạn kế hoạch chỉ tạo ba tài liệu này; **chưa thực hiện migration, sửa code hoặc chạy test**. Quyền triển khai bên dưới có hiệu lực khi người dùng gửi goal tương ứng cho Antigravity.
- Chuẩn bắt buộc là **6000.6.9f1**; không chạy 6000.5.0f1 rồi dùng kết quả đó để ký vòng 2. Chỉ dẫn giữ 6000.5.0f1 trong tài liệu vòng 1 được thay thế ở điểm này.
- `D:/thietkegame` là nguồn chuẩn **chỉ đọc** khi triển khai. Không merge/copy kết quả về root, không commit thay đổi người dùng trong root.
- Giữ hai checkout: Runtime tại `D:/thietkegame/.fortress-workspaces/codex`, nhánh `fortress/codex`; Authoring tại `D:/thietkegame/.fortress-workspaces/antigravity`, nhánh **`fortress/antigravity-clean`**. Tên Codex ở path/branch là tên lịch sử; executor cả hai bên là Antigravity.
- Baseline lịch sử chung: `411a440ed12407971163d74e2f0a12bc70ad41b2`. Không chạy lại bootstrap/`--verify`, tạo clone từ root HEAD, reset/clean/stash mù hoặc làm lại work vòng 1.
- Đọc AGENTS trong checkout, AGENTS mới tại root, RTK được tham chiếu, skill Unity tương ứng, master, Tasks, plan và goal vòng 2. Vòng 2 chỉ thay version, executor, quyền migration và quy trình phối hợp được ghi rõ; các quy tắc khác giữ nguyên. Không sửa AGENTS/master/Tasks/preparation.json/goal vòng 1.
- Chạy shell qua `rtk`, UTF-8. Không tự viết `.meta`; Unity sinh metadata. Không dùng graph root để đọc source hiện tại của checkout không có index riêng.
- Không sửa Player/Enemies/Combat/CameraSystem hiện hữu, prefab gốc hoặc `Sandbox_Player.unity`; không làm lại movement/HP/damage. Overrides trên instance Player trong scene demo thuộc Runtime được phép, phải có rationale và test mask/grounding/damage.
- Được mở đúng Editor **đã cài**, resolve UPM trong checkout, import/generate/compile/EditMode/PlayMode/preview và tạo local commits/tags theo scope. Không cài/nâng phần mềm toàn máy, tự chấp nhận license/terms, đổi global MCP/config, push/publish hoặc đóng Editor của người dùng. Chưa có bằng chứng Editor 6000.6.9f1 cài sẵn thì phải kiểm tra, không tự khẳng định.
- Missing art/frame/audio/normal map giữ `draft`/`missing` với exact input. Không bịa artwork, animation, âm thanh, ảnh map tham chiếu hoặc approval layout. Không bỏ phần buildable vì một input tùy chọn thiếu.
- Không fetch/cherry-pick nhánh legacy `fortress/antigravity`. Handoff cũ ghi token từng lộ; chủ tài khoản xử lý rotate/revoke. Không in lại token, không claim token đã được thu hồi, không đưa raw credential vào report/commit.

## 2. Hiện trạng đã kiểm tra khi lập kế hoạch

| Phần | Bằng chứng ngày 16/11/2026 | Hệ quả |
|---|---|---|
| Root | ProjectVersion = 6000.6.9f1; manifest/lock khớp các package trong Tech Stack | Nguồn version/package cho migration |
| Runtime | HEAD vẫn ở baseline; FortressRuntime/FortressIntegration/report còn untracked; XML 32 EditMode + 24 PlayMode Passed trên 6000.5.0f1 | Giữ work, tạo checkpoint có scope; cần kiểm thử lại trên version mới |
| Authoring | HEAD `c99a7f1a0c3d15436a43abc636e758409b29b082`; clean branch có 5 delivery commits; 10 file đang sửa | Giữ revision F1/F2, không checkout đè hoặc import legacy |
| Authoring tests | XML lịch sử 16 Passed; source hiện có R07/R08 thành 18 tests, chưa được chạy lại | Kết quả cũ không chứng minh revision hiện tại |
| Compile findings | `GothicDungeonImporter.cs:1168` mất khai báo `TryReadPngDimensions`; `:1139` khai báo helper `internal`, caller ở `FortressAnimationAndPrefabGenerator.cs:78` thuộc assembly khác | Sửa trước phiên import/compile mới |
| Runtime findings | `DoorController.Close()` bỏ yêu cầu trong Opening; fixture không gán blocker/damage/input; validator hiện kiểm tra presence nhiều hơn wiring | Cần regression hành vi và negative validation tests |
| Authoring assets | Catalog 84 entries: 57 ready / 24 draft / 3 missing; path/GUID và 105 hook paths đã đối chiếu | Tái sử dụng, không sinh lại toàn bộ vô cớ; số lượng này là snapshot, không hardcode thành kết quả mới |
| Toàn hệ thống | C12–C14 chưa thực hiện; final demo/bốn guide/final report chưa có | Vòng 2 phải hoàn tất integration, không dừng ở hai nhánh riêng |

Các số dòng chỉ là điểm bắt đầu; worker phải đọc lại source/callers/asmdef hiện tại trước sửa. Findings mới được xử lý trong đúng owner và AC; không mở refactor toàn project.

## 3. Ownership và file map

Đường dẫn trong bảng tính tương đối với **checkout của owner**. Không có quyền ghi trực tiếp checkout bên kia.

| Owner | Được ghi |
|---|---|
| Authoring | Các vùng AG của Tasks mục 4: importer; `Tools/Editor/FortressProduction/**`; `Features/FortressPresentation/**`; `Art/Environment/{GothicDungeon,FortressGenerated}/**`; `Scenes/FortressAuthoring/**`; sáu guide authoring; `Reports/antigravity/**`; các settings/package AG đã sở hữu |
| Runtime | Các vùng Codex của Tasks mục 4: `Features/FortressRuntime/**`; `Tools/Editor/FortressIntegration/**`; `Scenes/FortressGameplay/**`; bốn guide runtime; `Reports/codex/**` |
| Authoring — ngoại lệ migration mới | `ProjectSettings/ProjectVersion.txt`, `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/ShaderGraphSettings.asset`, `Assets/Settings/UniversalRP.asset`, `docs/ProjectConfig.yaml`; cùng manifest/lock, Quality và URP global settings đã có trong quyền AG. Chỉ thay phần cần cho chuẩn 6000.6.9f1, có diff/rationale |
| Runtime — nhận migration | Được **cherry-pick commit migration đã xác minh**, không tự sửa các file migration/AG-owned. Nếu xung đột hoặc cần correction, gửi request cho Authoring |
| Điều phối dùng chung | Hai orchestrator được tạo/sử dụng duy nhất thư mục lease `D:/thietkegame/.fortress-workspaces/.round2-editor-lease/`; không coi đây là quyền sửa root project hoặc preparation.json |

Không cấp quyền copy nguyên ProjectSettings từ root. Giữ TagManager/Physics2D của Authoring và ID/matrix baseline; Runtime nhận chúng trong authoring delivery cuối. Thay đổi Unity tự phát sinh ngoài allowlist phải được kiểm tra và báo scope, không tự blanket revert hoặc ship.

Các file sửa chính đã xác định:

- Authoring: `Assets/_Project/Tools/Editor/GothicDungeonImporter.cs`; `Tools/Editor/FortressProduction/{FortressAnimationAndPrefabGenerator,FortressSpriteValidator}.cs`; `Features/FortressPresentation/Tests/FortressAuthoringPipelineTests.cs`; asmdef liên quan chỉ khi thật sự cần.
- Runtime: `Features/FortressRuntime/Interaction/DoorController.cs`; `Tests/{EditMode/MechanismTests,PlayMode/MechanismPlayTests,PlayMode/ActivationPlayTests}.cs`; `Tools/Editor/FortressIntegration/{FortressDevelopmentFixture,FortressRuntimeValidator}.cs`; `Tests/Editor/{FixtureTests,ValidatorTests}.cs`.
- Tích hợp mới trong Runtime: `Tools/Editor/FortressIntegration/FortressGameplayBinder.cs`, `Tests/Editor/IntegrationTests.cs`, các composed prefab dưới `Features/FortressRuntime/Prefabs/`, `Scenes/FortressGameplay/Demo/GothicFortress_Demo.unity`. Kiểm tra lại implementation trước tạo file mới; reuse nếu đã có lúc resume.
- Evidence vòng 2: `Reports/antigravity/round2/` và `Reports/codex/round2/`. Giữ XML/log vòng 1, cập nhật canonical status/handoff/catalog/runtime-validation để dashboard và bên nhận không đọc nhầm kết quả cũ.

## 4. Swarm và Editor lease

Mỗi swarm có orchestrator, các worker độc lập theo file và reviewer chỉ đọc. Orchestrator cấp ticket: ID, checkout/branch, inputs, allowed/forbidden files, API, AC/tests, dependency, Editor owner, kết quả phải trả. Không giao nguyên master cho mọi worker; không pin model không được user/tool cho phép; không để worker spawn writer đệ quy.

**Mặc định chỉ một Editor process/writer trên toàn hai swarm**, do lượt trước có báo cáo OOM. Code workers hai checkout vẫn có thể làm đồng thời khi không ở cửa sổ import/test. Orchestrator muốn mở Editor/UPM/compile/test/play/generate phải:

1. Acquire lease bằng thao tác tạo directory **atomic, fail nếu đã tồn tại**; sau đó ghi owner session, checkout, PID thật khi có, UTC và tác vụ. Không dùng API tạo-directory idempotent làm bằng chứng acquire.
2. Xác nhận đúng project path và 6000.6.9f1. Đóng băng mọi writer vào Assets/Packages/ProjectSettings của checkout đó, nhận ACK; file write cũng có thể gây auto-refresh. Reviewer và writer tài liệu vẫn làm được.
3. Chỉ một pipeline/test process; không mở batch Editor thứ hai trên checkout đang có Editor. Save và đóng chỉ instance do swarm sở hữu khi cần đổi cách chạy.
4. Lưu kết quả, exit code, console và trạng thái process. Release lease sau khi process/Editor do mình giữ đã kết thúc; không nhả khóa trong khi Editor còn import.
5. Nếu owner crash, xác minh PID/session không còn hoạt động rồi ghi recovery trước khi thu hồi lease. Không xóa khóa của writer đang sống, không busy-poll vô hạn. Chờ lease không đồng nghĩa test Failed.

Không giả Swarm hoặc reviewer. Nếu phiên không có native Swarm thật, báo `BLOCKED_ENV: SWARM_UNAVAILABLE`; giữ việc đọc/audit an toàn, không âm thầm thay executor đã được người dùng chọn.

## 5. Review Focus

| Điều kiện dễ bỏ sót | Hành vi phải chứng minh | Task |
|---|---|---|
| Cùng nâng Unity nhưng hai bộ package/settings khác nhau | Version, direct dependencies và hash migration payload khớp ở hai checkout | M2 / R2-00 |
| Release hold trước khi cửa mở xong, hoặc activate khi đang đóng | Trạng thái cuối theo yêu cầu mới nhất; collider/completion/events không sai hoặc lặp | R2-01 |
| Component tồn tại nhưng serialized reference chưa gán | Validator phát hiện sai wiring; fixture thật hoạt động | R2-02 |
| Texture thiếu, NPOT hợp lệ, manifest thật và đi khỏi one-way bằng chuyển động | Không false-positive import; native dimensions/pivot/order được giữ; physics test không teleport để giả walk-off | A2-02 |
| XML cũ, Assets bị sửa trong lúc test, thiếu report hoặc demo | Không ký Passed/READY từ evidence không cùng source; dashboard không coi thiếu file là pass | A2-04 / R2-03 / R2-06 |

## 6. Các task có đầu ra và proof

### P2-00 — Preflight và checkpoint, thực hiện riêng tại cả hai checkout

**Files:** report `round2/preflight.md`, `round2/status.md`; checkpoint các file hiện có trong ownership sau review. Không viết lại source.

- [ ] Ghi cwd/repo root/branch/HEAD/dirty paths, baseline manifest, tool Swarm/Unity thực có; kiểm tra AGENTS con. Re-read findings, không xóa revision hiện có.
- [ ] Review và checkpoint owned work. Runtime không stage `%ALLUSERSPROFILE%/` (file cục bộ lạc chỗ), credentials/caches, hoặc dùng `git add -A`. Authoring không lấy lịch sử legacy.
- [ ] Ghi rõ checkpoint là WIP chưa verified; historical tests thuộc 6000.5.0f1. Commit không đồng nghĩa nghiệm thu.
- [ ] Proof: diff/checkpoint paths đúng scope, baseline art/Player/Enemies/Combat/camera/sandbox được giữ; report có actual session ID/UTC. Handoff sau này phải bao gồm work Runtime vốn untracked.

### A2-01 — Sửa compile trong importer và API giữa assemblies

**Files:** `GothicDungeonImporter.cs`, caller trong `FortressAnimationAndPrefabGenerator.cs`; test authoring hiện có. **Consumes:** source F1/F2 chưa commit. **Produces:** `TryReadPngDimensions(string filePath, out int width, out int height)` được phục hồi; resolver dùng được từ assembly caller.

- [ ] Đối chiếu baseline method để phục hồi khai báo đúng cho body tại finding; tìm mọi caller của cả hai helper.
- [ ] Giữ ownership parsing/resolution ở importer. Cấp resolver đúng visibility cho caller thực tế (mở đúng method `public static` là lựa chọn nhỏ nhất nếu vẫn cần gọi chéo); không thêm vòng reference hoặc friend assembly chỉ để che thiết kế sai.
- [ ] Kiểm tra R07/R08 gọi được đường public thực; sau M2, compile cả `TilesetTools.Editor`, `FortressProduction.Editor`, test assembly trên 6000.6.9f1. Nếu compile chưa chạy, chỉ ghi CODE_READY.
- [ ] Reviewer xác minh method/body/caller/asmdef và commit sửa trong Authoring. Đây là prerequisite cho phiên Unity mới, không được đổi XML cũ thành kết quả mới.

### M2 — Migration chung do Authoring sở hữu

**Depends:** P2-00, A2-01. **Files:** migration allowlist mục 3; `Reports/antigravity/round2/migration-handoff.json`. **Produces:** một commit config-only `sourceMigrationCommit`, status `MIGRATION_READY` và hash payload cho Runtime.

- [ ] Xác minh ba input root bằng SHA-256 ở phụ lục. Khác hash thì báo input drift và chốt snapshot mới bằng evidence, không dùng ngầm bản khác hoặc yêu cầu root phải quay lại pristine.
- [ ] Đồng bộ byte-for-byte `ProjectVersion.txt`, `manifest.json`, `packages-lock.json` từ snapshot chuẩn vào Authoring. Đây là khôi phục bộ dependency đã resolve có sẵn; không tự đoán version/chỉnh dependency khác. Dùng UPM API cho thay đổi bổ sung thật sự cần, ghi lý do. Không copy root `.codex/config.toml` hoặc cấu hình tài khoản.
- [ ] Kiểm tra Editor đã cài bằng CLI; mở đúng 6000.6.9f1 dưới global lease. Để Unity resolve/import/migrate phần serialized cần thiết. Giữ TagManager/Physics2D hiện có. Ghi riêng compiler failures với package/runtime migration failures.
- [ ] Nếu upgrade lộ lỗi API ở code Authoring ngoài hai finding ban đầu, orchestrator kéo phần sửa compile của A2-02 lên trước gate M2, giao đúng file lease rồi compile lại. Không tạo vòng chờ A2-02 đợi MIGRATION_READY trong khi M2 đợi chính code fix đó; các code fix vẫn ở commit riêng ngoài config payload.
- [ ] Direct dependencies phải khớp root manifest. Nếu lock được Editor resolve lại, báo diff và hash cuối thực tế; không đổi version trực tiếp ngoài chuẩn. Chỉ đưa serialization delta cần thiết vào allowlist, cập nhật summary package/version trong ProjectConfig.
- [ ] Commit **chỉ config payload**, không gộp code sửa/asset generation/TagManager/Physics2D/report. Giữ hash source root, hash payload cuối, Editor binary/version, lệnh/exit code/compile evidence. Sau khi commit có SHA thật mới ghi migration-handoff.
- [ ] `MIGRATION_READY` chỉ khi đúng Editor đã resolve/import và compile được các assembly Authoring hiện tại; chưa đồng nghĩa authoring READY. Payload đóng băng; sửa tiếp cần revision migration và revalidation ở cả hai checkout.

`migration-handoff.json` tối thiểu: `contract: FORTRESS-1`, `round: 2`, `status: MIGRATION_READY`, `baselineCommit`, `sourceMigrationCommit`, `unityVersion: 6000.6.9f1`, `payloadFiles: [{path, sha256}]`, `rootInputHashes`, `verification: [{command, exitCode, evidencePath}]`, `signedBy`, `signedUtc`. SHA/PID/exit code phải lấy thực, không điền mẫu như kết quả.

### R2-00 — Runtime nhận đúng migration, không chờ full authoring

**Depends:** P2-00, M2. **Files:** chỉ cherry-pick payload migration; `Reports/codex/round2/migration-receipt.md`.

- [ ] Đọc migration-handoff ở Authoring; kiểm tra commit tồn tại trên clean branch, config-only và path allowlist, không chứa secret/code AG hoặc legacy ancestry.
- [ ] Fetch đúng commit/ref cần thiết, cherry-pick **chỉ sourceMigrationCommit** vào Runtime sau checkpoint. Không lấy luôn nhánh Authoring đang viết.
- [ ] Ghi source SHA và applied SHA thực tế (cherry-pick có thể đổi SHA); so sánh patch/payload hashes, không bắt hai commit khác parent phải có cùng SHA. Kiểm tra version/manifest/lock khớp receipt.
- [ ] Compile Runtime bằng 6000.6.9f1 dưới lease. Lỗi API do upgrade sửa trong Runtime ownership; lỗi package/shared config gửi Authoring correction. Không lén sửa settings để pass.

### R2-01 — Cửa giữ yêu cầu cuối khi đang transition

**Files:** `DoorController.cs`, `MechanismTests.cs`, `MechanismPlayTests.cs`, `ActivationPlayTests.cs`. **Consumes/produces:** giữ API `Open()`, `Close()`, `SetActive(bool)`, `CompleteOpening()`, `CompleteClosing()`; không tạo controller mới.

- [ ] Thêm regression trước sửa: mở rồi `SetActive(false)` khi Opening; đóng rồi `SetActive(true)` khi Closing; true/false/true nhanh; request lặp; locked không mở trái phép.
- [ ] Chọn semantics cụ thể: hoàn tất transition đang chạy rồi thực hiện **desired state mới nhất**, không đảo animation giữa chừng. `SetActive/Open/Close` cập nhật desired state kể cả khi bận; event completion chỉ xử lý phase hiện tại. Collider đổi đúng completion, không mở sớm hoặc phát event lặp. Interaction spam không làm loot/event lặp.
- [ ] PlayMode test qua hold lever và pressure plate với timeout thực khác 0 (dùng 0.35s hiện có), release trước completion; assert final state Closed, blocker enabled. Test activate trong Closing về Open, blocker disabled; giữ các test lock/re-entry cũ.
- [ ] Chạy focused tests rồi full Runtime suite tại R2-03; reviewer đọc callback/re-entry/lifecycle trước commit.

### R2-02 — Sửa fixture và validator cùng một owner

**Files:** `FortressDevelopmentFixture.cs`, `FortressRuntimeValidator.cs`, `FixtureTests.cs`, `ValidatorTests.cs`; PlayMode regression trong `Features/FortressRuntime/Tests/PlayMode/FixturePlayTests.cs` nếu chưa có equivalent.

**Interfaces:** giữ `BuildFixtureRoot(Transform parent = null)`, `CreateAndSaveFixtureScene(string scenePath)`, `ValidateRoot(GameObject, string)`, `ValidateScene(Scene, string)`, `WriteReport(...)` và FORTRESS-1 JSON.

- [ ] Negative tests trước sửa: door có Collider nhưng `_blockingColliders` rỗng/sai; trap `_damage` null; actor không có input usable; hazard không có trigger phù hợp; missing script/reference. Assert check tương ứng không được `passed`.
- [ ] Gán blocker array/lock, trap damage, interaction Input System action hiện có, lever/plate→door/platform/trap events. Nếu action tạo bằng code, fixture phải sở hữu/dispose đúng vòng đời và validator nhận biết nguồn runtime thật; không làm hỏng map input chung. Tạo fixture hoạt động độc lập từ baseline+Runtime, không reference presentation AG mới.
- [ ] Fixture có actor/collision setup dùng được, visual development rõ nhãn (reuse sprite baseline nếu phù hợp), không có SpriteRenderer trống rồi mô tả như demo hình ảnh. Audio clip thiếu là optional silent/missing-input; thiếu input gameplay/reference bắt buộc không được gộp thành success.
- [ ] Validator kiểm serialized reference đúng component/collider đích, damage phase link, usable input, body/layer/trigger và missing script; negative test phải FAIL trước fix và PASS sau fix. Không chỉ kiểm component presence, cũng không phạt cấu hình optional hợp lệ.
- [ ] PlayMode proof trên fixture thật: interaction mở/đóng door và collider; hold release; trap không damage lúc Retracted và có damage đúng phase; existing Health/i-frame vẫn được tôn trọng. Save `Fortress_DevFixture.unity` vào Runtime scene scope bằng Unity và kiểm lại serialized refs sau reload.

### A2-02 — Chạy và sửa regression authoring trên version mới

**Depends:** M2. **Files:** các file Authoring trong mục 3; tests hiện có; generated asset/config chỉ khi cần sửa đúng owner.

- [ ] Chạy suite đầy đủ hiện có: A02–A13, R01–R08 (18 test methods tại thời điểm lập plan; số case thực lấy từ XML). Bảo toàn F1 missing texture rejection và F2 continuous walk-off; không xóa/skip regression để xanh.
- [ ] R07 phải đọc manifest thật: missing/unreadable texture/frame là error và không được thêm verified/imported. R08 phải import/slice/build artifact thật với NPOT 96x24/54x26, pivot (0.25,0.75), fps=12, loop=false, order=[2,0,1], native size và GUID ổn định qua rerun.
- [ ] R03 jump-through/land/walk-off dùng physics stepping; chứng minh xuất phát đang chạm platform, di chuyển ngang qua mép, mất contact, rơi và chạm floor; không teleport thay bước walk-off. Giữ test multi-collider và collision không trùng.
- [ ] Sửa API upgrade/regression trong đúng layer đang sở hữu. Test generator lần hai, customized prefab/config, missing source, 47 topology, static variation và dashboard missing-report behavior. Không tái tạo toàn bộ assets nếu kết quả cũ vẫn đúng.
- [ ] Commit code/assets đã qua review; XML/log cũ giữ nguyên, evidence mới nằm round2.

### A2-03 — Preview, catalog và coverage authoring A00–A13

**Depends:** A2-02. **Files:** Authoring preview/prefab/catalog/sáu guide và report.

- [ ] Chạy pipeline/validators trên 6000.6.9f1 có dry-run và guard customize; kiểm reload refs/GUID, 12 tilemaps, solid/one-way/decoration collision, sorting/light/pixel clarity, ambient toggle, animation/frame assignments.
- [ ] Xem preview Unity thực; lưu screenshot mới từ đúng checkout/version. Không dùng ảnh cũ hoặc browser để thay bằng chứng version mới. Ghi rõ inspected/inventory-only.
- [ ] Catalog FORTRESS-1 có stable IDs, GUID/path/hook/Animator thực; ready/draft/missing và input thiếu chính xác. Không hardcode 84/57/24/3 như kết quả mong muốn; reconcile counts với assets hiện tại.
- [ ] Đối chiếu **từng A00–A13** và master sections liên quan trong `round2/acceptance-matrix.md`: requirement, implementation, serialized artifact, test/visual proof, current status, missing input. Hoàn tất mọi phần buildable, không giới hạn vào bốn finding của review.
- [ ] Sáu guide mô tả tool/6000.6.9f1 thực tế; phần thiếu art giữ riêng, không làm mất full pipeline.

### R2-03 — Runtime gate độc lập C00–C11

**Depends:** R2-00, R2-01, R2-02. **Files:** report/Runtime code/test trong ownership.

- [ ] Chạy toàn bộ Runtime EditMode + PlayMode trên 6000.6.9f1, bao gồm 32+24 case lịch sử còn applicable và regression mới. Không dùng tổng case để thay proof từng AC.
- [ ] Xác minh real Player rider ngang/dọc/one-way drop/jump-off, hazard multi-hurtbox/i-frame/perfect dodge, chest loot một lần, destructible chết một lần, trap phase/reset, optional audio. Test fixture mới phải có mặt trong discovery/XML.
- [ ] Đối chiếu **từng C00–C11**; đưa C11 về trạng thái cần reverify cho tới khi fixture/validator sửa và có proof. Xuất canonical runtime-validation FORTRESS-1 và round2 evidence, không gộp branch pass thành integrated pass.
- [ ] Commit owned runtime work + Unity metadata. Nếu authoring final chưa READY: ghi `READY_FOR_AUTHORING_HANDOFF`, đưa next-action cụ thể và resume instructions; không báo toàn hệ thống xong, không tự làm AG assets.

### A2-04 — Freeze bàn giao Authoring

**Depends:** A2-03, reviewer độc lập. **Files:** canonical `Reports/antigravity/{status.md,asset-catalog.json,authoring-handoff.md}` và round2 evidence.

- [ ] Kiểm full delivery diff/history từ baseline: đúng ownership + ngoại lệ migration; không legacy/secret/cache/unrelated root work. Ghi source hashes và commit code/assets cuối đã test.
- [ ] `authoring-handoff.md` ghi 6000.6.9f1, migration SHA, ordered delivery commits, exact missing inputs, APIs/events/completion/fallback, PPU/layers/matrix, results, reviewer/session/UTC.
- [ ] Commit evidence/docs, rồi tạo local tag mới **`fortress-authoring-r2-ready-v1`** trỏ HEAD cuối. Handoff ghi `deliveryRef: refs/tags/fortress-authoring-r2-ready-v1` và `testedSourceCommit`; SHA delivered cuối được lấy bằng `git rev-parse` tag và báo trong kết quả thực thi. Không bịa SHA tự tham chiếu của commit chứa handoff.
- [ ] Chỉ ghi **READY** nếu actual current source đã compile/test/visual trên 6000.6.9f1; missing source được khai báo theo contract. Tag/HEAD bàn giao đóng băng; correction dùng commit/tag v2 mới, không move/amend tag v1.

### R2-04 — Nhận authoring và bind composed gameplay (C12)

**Depends:** R2-03, A2-04. **Files:** Runtime binder/composed prefabs/integration tests và nhận delivery commits AG.

- [ ] Resolve deliveryRef sang SHA, kiểm handoff READY/version/baseline/scope/hash, freeze danh sách commit rồi mới fetch/cherry-pick. Import 5 delivery commits clean đã có cùng các correction vòng 2; bỏ qua **migration payload đã áp dụng**, ghi mapping source→applied SHA. Không cherry-pick baseline/report SHA placeholder/nhánh đang viết.
- [ ] Nếu conflict: không ours/theirs mù, không sửa implementation AG. Ghi path/hunk và expected/actual trong `Reports/codex/round2/authoring-change-request.md`; owner Authoring cấp correction commit/recipe. Giữ local Runtime work; phần độc lập tiếp tục.
- [ ] Binder reuse catalog IDs/GUID/Animator/hook thật; không sửa visual source prefab. Tạo composition/variant trong Runtime, giữ refs qua reload và rerun/customization. Bind trạng thái bool static fallback đúng giới hạn, không đợi AnimationEvent không tồn tại.
- [ ] Integration tests cho lever/plate→gate/elevator/trap, chest/destruction callbacks một lần, layer masks/grounding/one-way/rider, required refs và generation lần hai. Không sửa Player prefab gốc hoặc đổi shared damage contract.

### R2-05 — Demo thật (C13)

**Depends:** R2-04. **Files:** `Assets/_Project/Scenes/FortressGameplay/Demo/GothicFortress_Demo.unity`, prefab/test liên quan trong Runtime.

- [ ] Dựng bằng art thật và existing Player/camera; đủ mọi mục master 21: walls/ground/ceilings/platforms/vertical/one-way/gate/lever/chest/torch/magic/hazard/mover/decor/background/foreground/light.
- [ ] Chạy demo dưới lease, chứng minh frameCount tiến và console không có lỗi liên quan; kiểm input, short hold, loot, damage/i-frame, destruction, rider, one-way và pixel clarity bằng quan sát + assertions phù hợp. Screenshot tĩnh không thay proof movement.
- [ ] Lưu screenshots/log/physics observations theo đúng version/source. Đây là demo framework để user duyệt, không claim map khớp ảnh hoặc bố cục đã được user xác nhận.

### R2-06 — Gate toàn hệ thống và báo cáo nghiệm thu (C14)

**Depends:** R2-05, reviewer độc lập. **Files:** bốn guide Runtime, canonical status/runtime-validation/handoff và `Reports/codex/final-validation.md`; evidence/matrix round2.

- [ ] Compile và relevant full EditMode/PlayMode trên snapshot đã tích hợp; chạy authoring validators và kiểm các authoring tests applicable sau import. So sánh hashes của source/assets được test với bản bàn giao; thay đổi sau test làm proof liên quan hết hiệu lực.
- [ ] Đối chiếu từng C00–C14, A00–A13 và từng checkbox master 26–27, nêu code/config/test/visual evidence. Kiểm đủ **10 guides**, prefab/scene/controller/frame/RuleTile refs, missing scripts/sprites/collision/sorting; dashboard phải đọc kết quả thật.
- [ ] Xuất `final-validation.md`: VERIFIED / FAILED / BLOCKED_INPUT / BLOCKED_ENV / NOT_RUN phân biệt rõ, findings còn mở, missing art, test exit/XML, tested source SHA, reviewer/session/UTC. Chưa build thì ghi chưa build; không bắt build Windows/WebGL/Android ngoài mục tiêu đã giao.
- [ ] Chỉ báo **READY_FOR_USER_REVIEW** khi phần buildable không còn blocker, integration/demo/test có proof mới; đây không phải tự duyệt nghiệm thu thay user. Kết quả cuối vẫn trong checkout Runtime, không tự áp về root.

## 7. Commands và evidence

Orchestrator xác minh `unity --help`/`unity test --help` và skills hiện tại trước dùng. Các lệnh sau là mẫu thực thi đã xác nhận flag, **chỉ chạy trong goal triển khai và khi giữ lease**; tạo thư mục report trước. Với `unity test`, project là positional; với lệnh điều khiển live Editor luôn thêm `--project-path` đúng checkout.

```powershell
rtk proxy unity editors --installed --format json
rtk proxy unity status --project-path D:/thietkegame/.fortress-workspaces/antigravity --json
rtk proxy unity test D:/thietkegame/.fortress-workspaces/antigravity --editor-version 6000.6.9f1 --mode EditMode --output D:/thietkegame/.fortress-workspaces/antigravity/docs/Fortress/Reports/antigravity/round2/editmode-results.xml --format json
rtk proxy unity test D:/thietkegame/.fortress-workspaces/codex --editor-version 6000.6.9f1 --mode EditMode --output D:/thietkegame/.fortress-workspaces/codex/docs/Fortress/Reports/codex/round2/editmode-results.xml --format json
rtk proxy unity test D:/thietkegame/.fortress-workspaces/codex --editor-version 6000.6.9f1 --mode PlayMode --output D:/thietkegame/.fortress-workspaces/codex/docs/Fortress/Reports/codex/round2/playmode-results.xml --format json
```

Lưu stdout/stderr và Editor log đã lọc secret; lấy actual exit code. Không giả flag `--log-file` cho `unity test` nếu help không cung cấp; dùng đường log CLI trả về hoặc passthrough Editor args được help/skill xác nhận. Không truyền `--allow-install` hoặc tự nhận license. Run focused tests trước full gate; chỉ rerun sau thay đổi/failure/new unresolved risk. Nếu từng dùng recovery env, truyền giá trị Windows thực cho process, không chuỗi `%ALLUSERSPROFILE%` chưa expand và không sửa global env.

`round2/evidence-index.json` mỗi bên ghi Editor version/path, tested source commit, hash danh sách Assets/Packages/ProjectSettings liên quan (không hash chính reports), command/UTC/exit/XML/log/screenshots, reviewer và unresolved inputs. Không dùng timestamp/XML tổng Passed đơn độc để chứng minh source hiện tại.

## 8. Lịch chạy và điều kiện dừng

```text
Authoring: P2-00 -> A2-01 -> M2 -> A2-02 -> A2-03 -> A2-04 READY
                             |                         |
Runtime:   P2-00 -> R2-01 --+ |                         |
                   R2-02 --+-R2-00 -> R2-03 ----------> R2-04 -> R2-05 -> R2-06
```

R2-01 và R2-02 có file leases riêng, làm song song với nhánh Authoring; compile/test đợi migration và Editor lease. Hai worker không cùng sửa `DoorController` hoặc file test chung. A2-01 và A2-02 cùng vùng importer/test phải chạy nối tiếp hoặc bàn giao lease rõ ràng. Mặc định một Editor nên các khoảng chạy Unity xếp hàng, không mặc định hai project cần mở đồng thời.

Chặn license/Editor/package/OOM: ghi BLOCKED_ENV, giữ công việc, làm phần độc lập; không tắt Editor/process không thuộc mình hoặc thay cấu hình máy để vượt chặn. Chờ handoff: không busy-wait; ghi mốc resume và kết thúc lượt nhánh. Khi nhận lại **cùng goal vòng 2**, đọc ledger rồi tiếp tục phần chưa verified, không làm lại việc còn proof hợp lệ.

## 9. Phụ lục snapshot đầu vào

Đọc tại root lúc lập plan, không phải kết quả migration đã chạy:

| File | SHA-256 |
|---|---|
| `ProjectSettings/ProjectVersion.txt` | `ebaccfd103d4983934b050d2ef8652fcbc3fd5eade2304268930fc6e4dfccf84` |
| `Packages/manifest.json` | `658fa5a286a0ab53f70afb0d40d39447232c8a21281373f177f8425470fd4328` |
| `Packages/packages-lock.json` | `69b3bba7ba9ea30cd2809d2bc020ab2d1b6509afb449e9a18ca49d1c2e333e7b` |

Các direct dependencies lấy **toàn bộ manifest** ở hash trên, gồm cả modules. Điểm chính: Animation 16.0.1; Aseprite 6.0.0; PSD Importer 15.0.1; SpriteShape 16.0.0; Tilemap Extras 9.0.1; Input System 1.20.1; URP 17.6.0; Test Framework 1.8.0; Cinemachine 6.6.0; UGUI 2.6.0; Pipeline 1.0.0. Không suy package từ ProjectConfig cũ.
