'use server'
import { redirect } from 'next/navigation'
import {userClient} from "@/api-clients";

export async function createPlayer(formData: FormData) {
    await userClient.createUser(formData.get('name') as string, "")
    redirect('/')
}