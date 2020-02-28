import Person from "./Person";

export type CommentOrigin = "Company" | "Contractor"

type Comment = {
    created: Date;
    updated?: Date;
    createdBy: Person;
    updatedBy: Person | null;
    content: string;
    origin: CommentOrigin;
}

export default Comment;