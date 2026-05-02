import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

type CreateClassRequest = {
  className?: string;
  studentPassword?: string;
  students?: string[];
};

type CreatedStudent = {
  fullName: string;
  email: string;
  password: string;
  profileId: string;
};

type FailedStudent = {
  fullName: string;
  reason: string;
};

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};

const supabaseUrl = Deno.env.get("SUPABASE_URL");
const serviceRoleKey = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY");
const emailDomain = Deno.env.get("STUDENT_EMAIL_DOMAIN") ?? "conta.local";

if (!supabaseUrl || !serviceRoleKey) {
  throw new Error("Missing SUPABASE_URL or SUPABASE_SERVICE_ROLE_KEY");
}

const supabaseAdmin = createClient(supabaseUrl, serviceRoleKey, {
  auth: {
    autoRefreshToken: false,
    persistSession: false,
  },
});

Deno.serve(async (request) => {
  if (request.method === "OPTIONS") {
    return jsonResponse({}, 200);
  }

  if (request.method !== "POST") {
    return jsonResponse({ error: "Method not allowed" }, 405);
  }

  try {
    const teacherToken = getBearerToken(request);
    if (!teacherToken) {
      return jsonResponse({ error: "Missing teacher access token" }, 401);
    }

    const payload = await readPayload(request);
    const validationError = validatePayload(payload);
    if (validationError) {
      return jsonResponse({ error: validationError }, 400);
    }

    const { data: teacherAuth, error: teacherAuthError } = await supabaseAdmin.auth.getUser(teacherToken);
    if (teacherAuthError || !teacherAuth.user) {
      return jsonResponse({ error: "Invalid teacher access token" }, 401);
    }

    const { data: teacherProfile, error: teacherProfileError } = await supabaseAdmin
      .from("profiles")
      .select("id, email, full_name, role")
      .eq("id", teacherAuth.user.id)
      .single();

    if (teacherProfileError || !teacherProfile) {
      return jsonResponse({ error: "Teacher profile was not found" }, 403);
    }

    if (teacherProfile.role !== "teacher") {
      return jsonResponse({ error: "Only teacher accounts can create classes" }, 403);
    }

    const { data: createdClass, error: classError } = await supabaseAdmin
      .from("classes")
      .insert({
        teacher_id: teacherProfile.id,
        name: payload.className!.trim(),
      })
      .select("id, name, teacher_id")
      .single();

    if (classError || !createdClass) {
      return jsonResponse({ error: classError?.message ?? "Failed to create class" }, 500);
    }

    const createdStudents: CreatedStudent[] = [];
    const failedStudents: FailedStudent[] = [];

    for (const rawFullName of payload.students!) {
      const fullName = normalizeDisplayName(rawFullName);
      if (!fullName) {
        continue;
      }

      try {
        const email = await generateUniqueStudentEmail(fullName);

        const { data: authResult, error: createUserError } = await supabaseAdmin.auth.admin.createUser({
          email,
          password: payload.studentPassword!,
          email_confirm: true,
          user_metadata: {
            full_name: fullName,
            role: "student",
            class_id: createdClass.id,
          },
        });

        if (createUserError || !authResult.user) {
          throw new Error(createUserError?.message ?? "Failed to create auth user");
        }

        const { error: profileError } = await supabaseAdmin
          .from("profiles")
          .upsert({
            id: authResult.user.id,
            email,
            full_name: fullName,
            role: "student",
            class_id: createdClass.id,
          }, {
            onConflict: "id",
          });

        if (profileError) {
          await supabaseAdmin.auth.admin.deleteUser(authResult.user.id);
          throw new Error(profileError.message);
        }

        const { error: classStudentError } = await supabaseAdmin
          .from("class_students")
          .upsert({
            class_id: createdClass.id,
            student_id: authResult.user.id,
            display_name: fullName,
          }, {
            onConflict: "class_id,student_id",
          });

        if (classStudentError) {
          await supabaseAdmin.auth.admin.deleteUser(authResult.user.id);
          throw new Error(classStudentError.message);
        }

        createdStudents.push({
          fullName,
          email,
          password: payload.studentPassword!,
          profileId: authResult.user.id,
        });
      } catch (error) {
        failedStudents.push({
          fullName,
          reason: error instanceof Error ? error.message : "Unknown error",
        });
      }
    }

    return jsonResponse({
      classId: createdClass.id,
      className: createdClass.name,
      teacherId: createdClass.teacher_id,
      createdStudents,
      failedStudents,
    }, failedStudents.length > 0 ? 207 : 201);
  } catch (error) {
    return jsonResponse({
      error: error instanceof Error ? error.message : "Unexpected server error",
    }, 500);
  }
});

async function readPayload(request: Request): Promise<CreateClassRequest> {
  try {
    return await request.json();
  } catch {
    throw new Error("Invalid JSON body");
  }
}

function validatePayload(payload: CreateClassRequest): string | null {
  if (!payload.className?.trim()) {
    return "className is required";
  }

  if (!payload.studentPassword || payload.studentPassword.length < 6) {
    return "studentPassword must contain at least 6 characters";
  }

  if (!Array.isArray(payload.students) || payload.students.length === 0) {
    return "students must be a non-empty array";
  }

  const normalizedStudents = payload.students.map(normalizeDisplayName).filter(Boolean);
  if (normalizedStudents.length === 0) {
    return "students must contain at least one valid full name";
  }

  return null;
}

function getBearerToken(request: Request): string | null {
  const authorization = request.headers.get("Authorization");
  if (!authorization?.startsWith("Bearer ")) {
    return null;
  }

  return authorization.slice("Bearer ".length).trim();
}

function normalizeDisplayName(value: string): string {
  return value.trim().replace(/\s+/g, " ");
}

async function generateUniqueStudentEmail(fullName: string): Promise<string> {
  const baseSlug = transliterateToEmailSlug(fullName);

  for (let attempt = 0; attempt < 50; attempt++) {
    const randomNumber = crypto.getRandomValues(new Uint32Array(1))[0] % 90 + 10;
    const email = `${baseSlug}${randomNumber}@${emailDomain}`;

    const { data, error } = await supabaseAdmin
      .from("profiles")
      .select("id")
      .eq("email", email)
      .maybeSingle();

    if (error) {
      throw new Error(error.message);
    }

    if (!data) {
      return email;
    }
  }

  throw new Error(`Could not generate unique email for ${fullName}`);
}

function transliterateToEmailSlug(value: string): string {
  const map: Record<string, string> = {
    "\u0430": "a",
    "\u0431": "b",
    "\u0432": "v",
    "\u0433": "g",
    "\u0434": "d",
    "\u0435": "e",
    "\u0451": "e",
    "\u0436": "zh",
    "\u0437": "z",
    "\u0438": "i",
    "\u0439": "i",
    "\u043a": "k",
    "\u043b": "l",
    "\u043c": "m",
    "\u043d": "n",
    "\u043e": "o",
    "\u043f": "p",
    "\u0440": "r",
    "\u0441": "s",
    "\u0442": "t",
    "\u0443": "u",
    "\u0444": "f",
    "\u0445": "h",
    "\u0446": "c",
    "\u0447": "ch",
    "\u0448": "sh",
    "\u0449": "sh",
    "\u044a": "",
    "\u044b": "y",
    "\u044c": "",
    "\u044d": "e",
    "\u044e": "yu",
    "\u044f": "ya",
  };

  const transliterated = value
    .toLowerCase()
    .split("")
    .map((char) => map[char] ?? char)
    .join("");

  const slug = transliterated
    .normalize("NFKD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/[^a-z0-9]/g, "");

  return slug || "student";
}

function jsonResponse(body: unknown, status: number) {
  return new Response(JSON.stringify(body), {
    status,
    headers: {
      ...corsHeaders,
      "Content-Type": "application/json",
    },
  });
}
