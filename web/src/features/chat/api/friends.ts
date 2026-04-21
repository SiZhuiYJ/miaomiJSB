import http from "@/libs/http";
import type {
  CreateFriendRequestPayload,
  FriendRequest,
  Friendship,
  RejectFriendRequestPayload,
} from "../types";

export async function getFriends() {
  return await http.get<Friendship[]>("/mm/friends");
}

export async function getFriendRequests(status?: string) {
  return await http.get<FriendRequest[]>("/mm/friends/requests", {
    params: status ? { status } : undefined,
  });
}

export async function createFriendRequest(payload: CreateFriendRequestPayload) {
  return await http.post<FriendRequest>("/mm/friends/requests", payload);
}

export async function acceptFriendRequest(requestId: number) {
  return await http.post<FriendRequest>(`/mm/friends/requests/${requestId}/accept`);
}

export async function rejectFriendRequest(
  requestId: number,
  payload?: RejectFriendRequestPayload,
) {
  return await http.post<FriendRequest>(
    `/mm/friends/requests/${requestId}/reject`,
    payload ?? {},
  );
}
