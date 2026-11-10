import { z } from 'zod';

/** Mirrors backend SecurityConstants.MINIMUM_PASSWORD_LENGTH. */
export const MIN_PASSWORD_LENGTH = 12;

const passwordField = z
  .string()
  .min(MIN_PASSWORD_LENGTH, `Password must be at least ${MIN_PASSWORD_LENGTH} characters`);

export const createUserSchema = z.object({
  username: z.string().min(3, 'Username must be at least 3 characters'),
  email: z.string().email('Enter a valid email address'),
  password: passwordField,
  firstName: z.string().optional(),
lastName: z.string().optional(),
  roles: z.array(z.string()).min(1, 'Select at least one role'),
  organizationId: z.string().optional().nullable(),
  departmentId: z.string().optional().nullable(),
});

export const updateUserSchema = z.object({
  email: z.string().email('Enter a valid email address'),
  firstName: z.string().optional(),
  lastName: z.string().optional(),
  roles: z.array(z.string()).min(1, 'Select at least one role'),
  organizationId: z.string().optional().nullable(),
  departmentId: z.string().optional().nullable(),
});

export const resetPasswordSchema = z.object({
  newPassword: passwordField,
});

export type CreateUserForm = z.infer<typeof createUserSchema>;
export type UpdateUserForm = z.infer<typeof updateUserSchema>;
