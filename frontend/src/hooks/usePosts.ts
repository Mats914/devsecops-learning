import { useState, useCallback } from 'react';
import { postsApi } from '../api/client';
import type { Post, PostsPagedResponse, PageParams, ApiError } from '../types';

// Hook för att hämta och hantera en paginerad lista med inlägg
export function usePosts() {
  const [data,    setData]    = useState<PostsPagedResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error,   setError]   = useState('');

  const fetch = useCallback(async (params: Partial<PageParams> = {}) => {
    setLoading(true);
    setError('');
    try {
      const res = await postsApi.getAll(params);
      setData(res);
    } catch (e) {
      setError((e as ApiError).message);
    } finally {
      setLoading(false);
    }
  }, []);

  // Tar bort lokalt direkt så listan känns snabb (optimistisk uppdatering)
  const deletePost = useCallback(async (id: number) => {
    await postsApi.delete(id);
    setData(prev => prev
      ? { ...prev, items: prev.items.filter(p => p.id !== id), totalCount: prev.totalCount - 1 }
      : prev);
  }, []);

  return { data, loading, error, fetch, deletePost };
}

// Hook för ett enskilt inlägg (t.ex. detaljsida)
export function usePost(id: number) {
  const [post,    setPost]    = useState<Post | null>(null);
  const [loading, setLoading] = useState(false);
  const [error,   setError]   = useState('');

  const fetch = useCallback(async () => {
    setLoading(true);
    try {
      setPost(await postsApi.getById(id));
    } catch (e) {
      setError((e as ApiError).message);
    } finally {
      setLoading(false);
    }
  }, [id]);

  return { post, loading, error, fetch };
}
