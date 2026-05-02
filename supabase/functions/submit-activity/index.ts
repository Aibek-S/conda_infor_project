import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

type SubmitActivityRequest = {
  activeWindow?: string;
  processes?: string[];
};

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};

const supabaseUrl = Deno.env.get("SUPABASE_URL");
const serviceRoleKey = Deno.env.get("SERVICE_ROLE_KEY") ?? Deno.env.get("SUPABASE_SERVICE_ROLE_KEY");
const minIntervalSeconds = Number(Deno.env.get("ACTIVITY_MIN_INTERVAL_SECONDS") ?? "5");

if (!supabaseUrl || !serviceRoleKey) {
  throw new Error("Missing SUPABASE_URL or SERVICE_ROLE_KEY");
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
    const studentToken = getBearerToken(request);
    if (!studentToken) {
      return jsonResponse({ error: "Missing student access token" }, 401);
    }

    const payload = await readPayload(request);
    const validationError = validatePayload(payload);
    if (validationError) {
      return jsonResponse({ error: validationError }, 400);
    }

    const { data: studentAuth, error: studentAuthError } = await supabaseAdmin.auth.getUser(studentToken);
    if (studentAuthError || !studentAuth.user) {
      return jsonResponse({ error: "Invalid student access token" }, 401);
    }

    const { data: studentProfile, error: studentProfileError } = await supabaseAdmin
      .from("profiles")
      .select("id, role")
      .eq("id", studentAuth.user.id)
      .single();

    if (studentProfileError || !studentProfile) {
      return jsonResponse({ error: "Student profile was not found" }, 403);
    }

    if (studentProfile.role !== "student") {
      return jsonResponse({ error: "Only student accounts can submit activity" }, 403);
    }

    const { data: classStudent, error: classStudentError } = await supabaseAdmin
      .from("class_students")
      .select("class_id")
      .eq("student_id", studentProfile.id)
      .order("created_at", { ascending: false })
      .limit(1)
      .maybeSingle();

    if (classStudentError) {
      return jsonResponse({ error: classStudentError.message }, 500);
    }

    if (!classStudent) {
      return jsonResponse({ error: "Student is not linked to a class" }, 409);
    }

    const { data: latestLog, error: latestLogError } = await supabaseAdmin
      .from("activity_logs")
      .select("created_at")
      .eq("student_id", studentProfile.id)
      .eq("class_id", classStudent.class_id)
      .order("created_at", { ascending: false })
      .limit(1)
      .maybeSingle();

    if (latestLogError) {
      return jsonResponse({ error: latestLogError.message }, 500);
    }

    if (latestLog?.created_at && isTooSoon(latestLog.created_at)) {
      return jsonResponse({
        error: `Activity can be submitted once every ${minIntervalSeconds} seconds`,
      }, 429);
    }

    const processList = normalizeProcesses(payload.processes!);
    const { data: insertedLog, error: insertError } = await supabaseAdmin
      .from("activity_logs")
      .insert({
        student_id: studentProfile.id,
        class_id: classStudent.class_id,
        active_window: payload.activeWindow?.trim() || null,
        process_list: processList,
      })
      .select("id, student_id, class_id, active_window, process_list, created_at")
      .single();

    if (insertError || !insertedLog) {
      return jsonResponse({ error: insertError?.message ?? "Failed to insert activity" }, 500);
    }

    return jsonResponse({
      ok: true,
      activity: insertedLog,
    }, 201);
  } catch (error) {
    return jsonResponse({
      error: error instanceof Error ? error.message : "Unexpected server error",
    }, 500);
  }
});

async function readPayload(request: Request): Promise<SubmitActivityRequest> {
  try {
    return await request.json();
  } catch {
    throw new Error("Invalid JSON body");
  }
}

function validatePayload(payload: SubmitActivityRequest): string | null {
  if (!Array.isArray(payload.processes)) {
    return "processes must be an array";
  }

  if (payload.processes.length > 300) {
    return "processes cannot contain more than 300 items";
  }

  if (payload.activeWindow && payload.activeWindow.length > 300) {
    return "activeWindow is too long";
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

function normalizeProcesses(processes: string[]): string[] {
  return [...new Set(
    processes
      .map((process) => process.trim())
      .filter(Boolean)
      .slice(0, 300)
  )].sort((a, b) => a.localeCompare(b));
}

function isTooSoon(createdAt: string): boolean {
  const lastTime = new Date(createdAt).getTime();
  const nextAllowedTime = lastTime + minIntervalSeconds * 1000;
  return Date.now() < nextAllowedTime;
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
