import mime from "mime";

export type PreviewFileTypes = 'image' | 'pdf' | 'other';

export const getFileType = (file_name: string): PreviewFileTypes => {
    const file_type = mime.getType(file_name);

    if (file_type && file_type.match('image.*')) {
        return 'image';
    } else if (file_type === 'application/pdf') {
        return 'pdf';
    }
    return 'other';
};


