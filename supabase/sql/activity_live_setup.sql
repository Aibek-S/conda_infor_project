create table if not exists public.activity_logs (
  id uuid primary key default gen_random_uuid(),
  student_id uuid not null references public.profiles(id) on delete cascade,
  class_id uuid not null references public.classes(id) on delete cascade,
  active_window text null,
  process_list jsonb not null default '[]'::jsonb,
  created_at timestamp with time zone not null default now()
);

create index if not exists activity_logs_class_student_created_idx
on public.activity_logs (class_id, student_id, created_at desc);

create index if not exists activity_logs_student_created_idx
on public.activity_logs (student_id, created_at desc);

alter table public.activity_logs enable row level security;

drop policy if exists "students can read own activity" on public.activity_logs;
create policy "students can read own activity"
on public.activity_logs
for select
to authenticated
using (student_id = auth.uid());

drop policy if exists "teachers can read class activity" on public.activity_logs;
create policy "teachers can read class activity"
on public.activity_logs
for select
to authenticated
using (
  exists (
    select 1
    from public.classes c
    where c.id = activity_logs.class_id
      and c.teacher_id = auth.uid()
  )
);

create table if not exists public.class_students (
  id uuid primary key default gen_random_uuid(),
  class_id uuid not null references public.classes(id) on delete cascade,
  student_id uuid not null references public.profiles(id) on delete cascade,
  display_name text not null,
  created_at timestamp with time zone not null default now(),
  unique (class_id, student_id)
);

create index if not exists class_students_class_id_idx
on public.class_students (class_id);

create index if not exists class_students_student_id_idx
on public.class_students (student_id);

alter table public.class_students enable row level security;

drop policy if exists "teachers can read own class students" on public.class_students;
create policy "teachers can read own class students"
on public.class_students
for select
to authenticated
using (
  exists (
    select 1
    from public.classes c
    where c.id = class_students.class_id
      and c.teacher_id = auth.uid()
  )
);

drop function if exists public.get_class_live_activity(uuid);

create function public.get_class_live_activity(target_class_id uuid)
returns table (
  student_id uuid,
  full_name text,
  email text,
  active_window text,
  process_list jsonb,
  last_seen timestamp with time zone,
  status text
)
language sql
security definer
set search_path = public
as $$
  with class_check as (
    select c.id
    from public.classes c
    where c.id = target_class_id
      and c.teacher_id = auth.uid()
  ),
  class_students_list as (
    select
      p.id as student_id,
      coalesce(p.full_name, cs.display_name) as full_name,
      p.email
    from public.class_students cs
    join public.profiles p on p.id = cs.student_id
    join class_check cc on cc.id = cs.class_id
    where cs.class_id = target_class_id
      and p.role = 'student'
  ),
  latest_logs as (
    select distinct on (al.student_id)
      al.student_id,
      al.active_window,
      al.process_list,
      al.created_at
    from public.activity_logs al
    join class_check cc on cc.id = al.class_id
    where al.class_id = target_class_id
    order by al.student_id, al.created_at desc
  )
  select
    s.student_id,
    s.full_name,
    s.email,
    l.active_window,
    coalesce(l.process_list, '[]'::jsonb) as process_list,
    l.created_at as last_seen,
    case
      when l.created_at is null then 'offline'
      when l.created_at > now() - interval '20 seconds' then 'online'
      else 'offline'
    end as status
  from class_students_list s
  left join latest_logs l on l.student_id = s.student_id
  order by s.full_name;
$$;

grant execute on function public.get_class_live_activity(uuid) to authenticated;
