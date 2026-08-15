import { Image, Stack, Text } from "@mantine/core";
import { useEffect, useState } from "react";
import { WebcamCapture } from "UI";

export function WebcamCaptureTest() {
    const [photoURL, setPhotoURL] = useState<string>();

    useEffect(() => () => {
        if (photoURL) URL.revokeObjectURL(photoURL);
    }, [photoURL]);

    const handleCapture = (file: File) => {
        setPhotoURL(previous => {
            if (previous) URL.revokeObjectURL(previous);
            return URL.createObjectURL(file);
        });
    };

    return (
        <Stack>
            <WebcamCapture onCapture={handleCapture} onCancel={() => { }} />
            {photoURL &&
                <>
                    <Text fw="700">Accepted photo</Text>
                    <Image src={photoURL} height="20rem" fit="contain" />
                </>
            }
        </Stack>
    );
}
