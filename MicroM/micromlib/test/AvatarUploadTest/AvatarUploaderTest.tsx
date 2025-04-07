import { AvatarUploader, EntityForm, useAvatarUploader, useEntityForm } from "UI";
import { MicroMClient } from "client";
import { useRef } from "react";
import { AvatarUploaderTestEntity } from "./AvatarUploaderTestEntity";

export function AvatarUploaderTest() {

    const client = useRef(new MicroMClient({ app_id: "", api_url: "" }));
    const entity = useRef(new AvatarUploaderTestEntity(client.current));

    const entityForm = useEntityForm({ entity: entity.current, initialFormMode: "add", getDataOnInit: false });

    const avatarAPI = useAvatarUploader({
        client: entity.current.API.client,
        fileProcessColumn: entity.current.def.columns.c_fileprocess_id,
        fileGUIDColumn: entity.current.def.columns.vc_fileguid,
        initialImageURL: "https://i.pravatar.cc/64?u=69",
        parentFormAPI: entityForm
    });

    return <EntityForm formAPI={entityForm}>
        <AvatarUploader
            API={avatarAPI} 
             />
    </EntityForm>
}
