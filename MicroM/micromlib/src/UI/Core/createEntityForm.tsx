import { ReactNode } from "react";
import { Entity, EntityDefinition } from "../../Entity";
import { AutoFiltersForm } from "../AutoFiltersForm";
import { AutoForm } from "../AutoForm";
import { FormOptions } from "./types";

export async function createEntityForm<T extends FormOptions<Entity<EntityDefinition>>>(props: T): Promise<ReactNode> {

    const { entity } = props;
    if (!entity.Form) throw new Error("Entity does not have a form");
    let entity_form: ReactNode;

    if (entity.Form === "AutoForm") {
        entity_form = <AutoForm {...props} />
    }
    else if (entity.Form === "AutoFiltersForm") {
        entity_form = <AutoFiltersForm {...props} />
    }
    else {
        entity_form = await entity.Form!.then(DynamicForm =>
            <DynamicForm {...props} />
        )
    }

    return entity_form;
};
